using System;
using mkLink;

namespace mkLink.Tests {

    /// <summary>
    /// The payloads below are the ones that used to work. Before the fix the
    /// command was assembled as MKLINK "<link>" "<target>" with nothing
    /// escaped, so a quote typed into either text box closed the quoting early
    /// and everything after it reached cmd.exe as syntax. The app is manifested
    /// requireAdministrator, so that syntax ran elevated.
    /// </summary>
    public class CommandLineTests {

        public static TheoryData<string> InjectionPayloads() {
            return new TheoryData<string> {
                "C:\\temp\\a\" & calc & \"b",
                "\" & calc & \"",
                "C:\\temp\" | calc | \"",
                "C:\\temp\" && calc && \"",
                "C:\\temp\" & shutdown /s /t 0 & \"",
                "C:\\temp\" > C:\\Windows\\System32\\drivers\\etc\\hosts \"",
                "C:\\temp\" < C:\\secret.txt \"",
                "\"",
                "a\"b",
            };
        }

        [Theory]
        [MemberData(nameof(InjectionPayloads))]
        public void Refuses_values_that_could_close_the_quoting(string payload) {
            Assert.NotNull(CommandLine.DescribeUnquotable(payload));
            Assert.Throws<ArgumentException>(() => CommandLine.Quote(payload));
            Assert.Throws<ArgumentException>(() => CommandLine.Build("MKLINK", payload, "C:\\target"));
        }

        [Theory]
        [InlineData("<")]
        [InlineData(">")]
        [InlineData("|")]
        [InlineData("*")]
        [InlineData("?")]
        public void Refuses_the_other_characters_a_path_cannot_hold(string forbidden) {
            Assert.NotNull(CommandLine.DescribeUnquotable("C:\\temp\\a" + forbidden + "b"));
        }

        [Theory]
        [InlineData("\n")]
        [InlineData("\r")]
        [InlineData("\t")]
        [InlineData("\0")]
        [InlineData("\u0007")]
        [InlineData("\u007f")]
        public void Refuses_control_characters(string control) {
            Assert.NotNull(CommandLine.DescribeUnquotable("C:\\temp\\a" + control + "b"));
        }

        [Fact]
        public void Refuses_null() {
            Assert.NotNull(CommandLine.DescribeUnquotable(null));
        }

        /// <summary>
        /// Everything cmd.exe would otherwise treat as an operator is legal in
        /// a Windows filename, so none of it may be refused. Quoting is what
        /// makes it inert, and quoting is enough because the characters that
        /// could escape the quotes are gone by now.
        /// </summary>
        [Theory]
        [InlineData("C:\\Program Files\\app")]
        [InlineData("C:\\temp\\a&b")]
        [InlineData("C:\\temp\\a&&b")]
        [InlineData("C:\\temp\\a^b")]
        [InlineData("C:\\temp\\a(b)c")]
        [InlineData("C:\\temp\\100%")]
        [InlineData("C:\\temp\\%PATH%")]
        [InlineData("C:\\temp\\!PATH!")]
        [InlineData("C:\\temp\\a;b,c=d")]
        [InlineData("C:\\temp\\a'b`c")]
        [InlineData("C:\\temp\\naïve résumé")]
        [InlineData("\\\\server\\share\\folder")]
        [InlineData("C:\\temp\\trailing\\")]
        [InlineData("")]
        public void Accepts_every_path_windows_accepts(string path) {
            Assert.Null(CommandLine.DescribeUnquotable(path));
            Assert.Equal("\"" + path + "\"", CommandLine.Quote(path));
        }

        /// <summary>
        /// A path ending in a separator is the reason the CommandLineToArgvW
        /// backslash rules are not applied here. Doubling the trailing
        /// backslash would be right for an executable re-splitting its own
        /// command line and wrong for an internal command like MKLINK, which
        /// never re-splits and would receive the doubled separator verbatim.
        /// </summary>
        [Fact]
        public void Leaves_a_trailing_separator_alone() {
            Assert.Equal("\"C:\\temp\\\"", CommandLine.Quote("C:\\temp\\"));
        }

        [Fact]
        public void Builds_the_command_with_one_token_per_argument() {
            Assert.Equal("MKLINK /J \"C:\\link dir\" \"C:\\target dir\"",
                CommandLine.Build("MKLINK /J", "C:\\link dir", "C:\\target dir"));
        }

        [Fact]
        public void Builds_a_command_with_no_arguments() {
            Assert.Equal("MKLINK", CommandLine.Build("MKLINK"));
        }

        [Fact]
        public void Rejects_a_missing_verbatim_command() {
            Assert.Throws<ArgumentNullException>(() => CommandLine.Build(null, "C:\\target"));
        }

        /// <summary>
        /// Whatever the input, the assembled line may only ever hold the quotes
        /// this class put there: two per argument, and nothing between the
        /// closing quote of one argument and the opening quote of the next
        /// except the single separating space.
        /// </summary>
        [Theory]
        [MemberData(nameof(InjectionPayloads))]
        public void Never_emits_a_line_with_unbalanced_quoting(string payload) {
            string line;
            try {
                line = CommandLine.Build("MKLINK", payload, "C:\\target");
            } catch (ArgumentException) {
                return; // refused outright, which is the stronger outcome
            }

            int quotes = 0;
            foreach (char c in line) {
                if (c == '"') {
                    quotes++;
                }
            }
            Assert.Equal(0, quotes % 2);
            Assert.Equal("MKLINK \"" + payload + "\" \"C:\\target\"", line);
        }
    }
}
