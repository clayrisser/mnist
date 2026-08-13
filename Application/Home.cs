using System;
using System.IO;
using System.Windows.Forms;
using System.Diagnostics;

namespace mkLink {
    public partial class Home : Form {

        private const string LearnMoreUrl =
            "https://learn.microsoft.com/windows-server/administration/windows-commands/mklink";
        private const string CreatedByUrl = "https://jamrizzi.com";

        private string initialFileFolderName = "";
        private bool targetIsFile = true;

        public Home(string fileName) {
            InitializeComponent();
            this.typeComboBox.SelectedIndex = 0;
            if (!string.IsNullOrEmpty(fileName)) { // Program run from another file
                SelectTarget(fileName);
                if (fileName.Exists()) {
                    SelectLink();
                }
            }
            UpdateValidation();
        }


        private void typeComboBox_SelectedIndexChanged(object sender, EventArgs e) {
            this.typeDescriptionLabel.Text = this.typeComboBox.Text.FindDescription();
            UpdateValidation();
        }


        private void targetSelectFileButton_Click(object sender, EventArgs e) {
            using (OpenFileDialog ofd = new OpenFileDialog()) {
                if (ofd.ShowDialog(this) == DialogResult.OK) {
                    SelectTarget(ofd.FileName);
                }
            }
        }


        private void targetSelectFolderButton_Click(object sender, EventArgs e) {
            using (FolderBrowserDialog fbd = new FolderBrowserDialog()) {
                if (fbd.ShowDialog(this) == DialogResult.OK) {
                    SelectTarget(fbd.SelectedPath);
                }
            }
        }


        private void selectLinkButton_Click(object sender, EventArgs e) {
            SelectLink();
        }


        private void createLinkButton_Click(object sender, EventArgs e) {
            string problem = DescribeProblem();
            if (problem != null) {
                MessageBox.Show(this, problem, "mkLink", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            // MKLINK [[/D] | [/H] | [/J]] Link Target
            string option = LinkTypeOption();
            string verbatim = "MKLINK" + (option.Length > 0 ? " " + option : "");

            CommandResult result;
            try {
                result = CMD.Execute(verbatim, this.linkTextBox.Text, this.targetTextBox.Text);
            } catch (ArgumentException ex) {
                // DescribeProblem already refuses everything CommandLine
                // refuses, so reaching this means the two disagree.
                MessageBox.Show(this, ex.Message, "mkLink",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (result.Succeeded) {
                MessageBox.Show(this, result.Message, "mkLink",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
            } else {
                MessageBox.Show(this, result.Message, "mkLink could not create the link",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            UpdateValidation();
        }


        private void targetTextBox_TextChanged(object sender, EventArgs e) {
            this.targetIsFile = this.targetTextBox.Text.IsFile();
            UpdateValidation();
        }


        private void linkTextBox_TextChanged(object sender, EventArgs e) {
            UpdateValidation();
        }


        private void learnMoreLinkLabel_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e) {
            OpenUrl(LearnMoreUrl);
        }


        private void createdByLinkLabel_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e) {
            OpenUrl(CreatedByUrl);
        }


        private void cancelButton_Click(object sender, EventArgs e) {
            this.Close();
        }


        /// <summary>
        /// The single place that decides whether the input is usable. Both
        /// text boxes and the type list run through it, so a field that has
        /// been corrected always re-enables the button.
        /// </summary>
        private void UpdateValidation() {
            this.linkGroupBox.Enabled = this.targetTextBox.Text.Exists();

            string problem = DescribeProblem();
            this.createLinkButton.Enabled = problem == null;

            bool untouched = this.targetTextBox.Text.Length == 0 && this.linkTextBox.Text.Length == 0;
            this.statusLabel.Text = (problem == null || untouched) ? "" : problem;
        }


        /// <summary>
        /// Why the link cannot be created, or null when it can.
        /// </summary>
        private string DescribeProblem() {
            string target = this.targetTextBox.Text;
            if (target.Length == 0) {
                return "Choose the file or folder to point at.";
            }
            // Ahead of FullPathOrNull, which only rejects these characters
            // while the project targets a framework old enough to still run
            // the legacy path checks.
            string unquotable = CommandLine.DescribeUnquotable(target);
            if (unquotable != null) {
                return unquotable;
            }
            if (target.FullPathOrNull() == null) {
                return "The target is not a valid path.";
            }
            if (!target.Exists()) {
                return "The target does not exist: " + target;
            }

            string link = this.linkTextBox.Text;
            if (link.Length == 0) {
                return "Choose where the link should be created.";
            }
            unquotable = CommandLine.DescribeUnquotable(link);
            if (unquotable != null) {
                return unquotable;
            }
            string fullLink = link.FullPathOrNull();
            if (fullLink == null) {
                return "The link is not a valid path.";
            }
            string parent = Path.GetDirectoryName(fullLink);
            if (parent == null) {
                return "The link cannot be the root of a drive.";
            }
            if (!Directory.Exists(parent)) {
                return "The folder for the link does not exist: " + parent;
            }
            if (fullLink.Exists()) {
                return "Something already exists at the link path: " + link;
            }

            return DescribeTypeProblem();
        }


        /// <summary>
        /// Hard links and junctions each only accept one kind of target, and
        /// MKLINK will happily create a junction that points at a file.
        /// </summary>
        private string DescribeTypeProblem() {
            switch (this.typeComboBox.Text) {
                case "Hard Link":
                    if (!this.targetIsFile) {
                        return "A hard link needs a file. Choose Directory Junction for a folder.";
                    }
                    break;
                case "Directory Junction":
                    if (this.targetIsFile) {
                        return "A directory junction needs a folder. Choose Hard Link for a file.";
                    }
                    break;
            }
            return null;
        }


        private string LinkTypeOption() {
            switch (this.typeComboBox.Text) {
                case "Hard Link":
                    return "/H";
                case "Directory Junction":
                    return "/J";
                default:
                    return this.targetIsFile ? "" : "/D";
            }
        }


        private void SelectTarget(string path) {
            this.targetTextBox.Text = path; // Raises TextChanged, which sets targetIsFile
            try {
                this.initialFileFolderName = Path.GetFileName(path.TrimEnd('\\', '/'));
            } catch (ArgumentException) {
                this.initialFileFolderName = "";
            }
        }


        private void SelectLink() {
            if (this.targetIsFile) { // Item is a file
                using (SaveFileDialog sfd = new SaveFileDialog()) {
                    sfd.FileName = this.initialFileFolderName;
                    sfd.OverwritePrompt = false; // MKLINK will not overwrite, and the check below says so
                    if (sfd.ShowDialog(this) == DialogResult.OK) {
                        this.linkTextBox.Text = sfd.FileName;
                    }
                }
            } else { // Item is a folder
                using (FolderBrowserDialog fbd = new FolderBrowserDialog()) {
                    if (fbd.ShowDialog(this) == DialogResult.OK) {
                        this.linkTextBox.Text = Path.Combine(fbd.SelectedPath, this.initialFileFolderName);
                    }
                }
            }
        }


        private static void OpenUrl(string url) {
            try {
                Process.Start(url);
            } catch (Exception e) {
                MessageBox.Show(null, "Could not open " + url + Environment.NewLine + e.Message,
                    "mkLink", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }
    }
}
