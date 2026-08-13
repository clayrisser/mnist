using System.Diagnostics;
using System.Text;

namespace mkLink {

    /// <summary>
    /// Everything a finished command has to say, including the two things the
    /// previous implementation threw away: standard error and the exit code.
    /// MKLINK reports its failures on standard error, so without them a failed
    /// link looked exactly like a successful one.
    /// </summary>
    class CommandResult {

        public readonly int ExitCode;
        public readonly string Output;
        public readonly string Error;

        public CommandResult(int exitCode, string output, string error) {
            this.ExitCode = exitCode;
            this.Output = output;
            this.Error = error;
        }

        public bool Succeeded {
            get { return this.ExitCode == 0; }
        }

        /// <summary>
        /// What to put in front of the user: whatever the command said,
        /// preferring the error stream, and never an empty dialog.
        /// </summary>
        public string Message {
            get {
                string message = (this.Error.Trim().Length > 0 ? this.Error : this.Output).Trim();
                if (message.Length > 0) {
                    return message;
                }
                return "The command exited with code " + this.ExitCode + " without reporting anything.";
            }
        }
    }


    class CMD {

        /// <summary>
        /// Runs <paramref name="verbatimCommand"/> through cmd.exe with each of
        /// <paramref name="arguments"/> appended as a quoted token.
        ///
        /// The split is the point: the command keeps its syntax in the first
        /// parameter, where only this program's own literals go, and every
        /// value a user can influence arrives separately and gets quoted by
        /// <see cref="CommandLine"/>. There is no overload that takes a
        /// pre-assembled command line, so a caller cannot concatenate its way
        /// past the quoting.
        ///
        /// /v:off pins delayed expansion off, so a !NAME! in a path stays a
        /// !NAME! even when the machine has turned expansion on by default in
        /// HKCU\Software\Microsoft\Command Processor.
        /// </summary>
        public static CommandResult Execute(string verbatimCommand, params string[] arguments) {
            string command = CommandLine.Build(verbatimCommand, arguments);

            ProcessStartInfo startInfo = new ProcessStartInfo("cmd.exe", "/v:off /c " + command);
            startInfo.UseShellExecute = false;
            startInfo.RedirectStandardOutput = true;
            startInfo.RedirectStandardError = true;
            startInfo.CreateNoWindow = true;

            StringBuilder output = new StringBuilder();
            StringBuilder error = new StringBuilder();

            using (Process process = new Process()) {
                process.StartInfo = startInfo;
                // Drain both pipes as they fill. Reading one of them to the end
                // before touching the other can deadlock once a pipe buffer is
                // full, and standard error was previously redirected and then
                // never read at all.
                process.OutputDataReceived += delegate (object sender, DataReceivedEventArgs e) {
                    if (e.Data != null) {
                        output.AppendLine(e.Data);
                    }
                };
                process.ErrorDataReceived += delegate (object sender, DataReceivedEventArgs e) {
                    if (e.Data != null) {
                        error.AppendLine(e.Data);
                    }
                };

                process.Start();
                process.BeginOutputReadLine();
                process.BeginErrorReadLine();
                process.WaitForExit();

                return new CommandResult(process.ExitCode, output.ToString(), error.ToString());
            }
        }
    }
}
