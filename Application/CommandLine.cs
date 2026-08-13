using System;
using System.Text;

namespace mkLink {

    /// <summary>
    /// Assembles command lines for cmd.exe so that the values in them stay
    /// values and never become syntax.
    ///
    /// MKLINK is an internal cmd.exe command, not an executable on disk, so
    /// cmd.exe cannot be cut out of the picture the way it could be for a real
    /// program. That makes cmd.exe's parser the only parser the arguments pass
    /// through, and its rules are the ones that have to be satisfied:
    ///
    ///   * A double quote toggles quoting. Inside a quoted run, the operators
    ///     &amp; | &lt; &gt; ( ) ^ and whitespace are all inert.
    ///   * A caret escapes the next character, but only OUTSIDE a quoted run.
    ///     Inside one it is an ordinary character, so caret escaping cannot be
    ///     layered on top of quoting.
    ///   * %NAME% is substituted before the command runs, quoted or not.
    ///   * !NAME! is substituted as well when delayed expansion is switched on.
    ///
    /// So a value is safe here when it is wrapped in quotes AND cannot contain
    /// a quote of its own to close that wrapping early. The second half is what
    /// <see cref="EnsureQuotable"/> enforces, and it costs nothing: every
    /// character it refuses is one Windows already refuses in a path.
    ///
    /// The CommandLineToArgvW backslash rules deliberately do not apply. They
    /// govern how a launched executable re-splits its own command line, and an
    /// internal command never gets re-split. Doubling backslashes for them here
    /// would corrupt every path that ends in a separator.
    /// </summary>
    static class CommandLine {

        /// <summary>
        /// Characters no Windows path may contain. A quote is the one that
        /// matters for injection; the rest come along because refusing them
        /// turns a confusing MKLINK error into a clear message.
        /// </summary>
        private const string ForbiddenCharacters = "\"<>|*?";

        /// <summary>
        /// Why <paramref name="value"/> cannot be put on a command line, or
        /// null when it can.
        /// </summary>
        public static string DescribeUnquotable(string value) {
            if (value == null) {
                return "The value is missing.";
            }
            for (int i = 0; i < value.Length; i++) {
                char c = value[i];
                if (ForbiddenCharacters.IndexOf(c) >= 0) {
                    return "A path cannot contain " + c + ".";
                }
                if (c < ' ' || c == '\u007f') {
                    return "A path cannot contain control characters.";
                }
            }
            return null;
        }

        /// <summary>
        /// Throws unless <paramref name="value"/> can be safely quoted. The
        /// caller is expected to have shown the user the same complaint
        /// already; this is the backstop that keeps a missed check from
        /// reaching cmd.exe.
        /// </summary>
        public static void EnsureQuotable(string value) {
            string problem = DescribeUnquotable(value);
            if (problem != null) {
                throw new ArgumentException(problem, "value");
            }
        }

        /// <summary>
        /// <paramref name="value"/> as a single cmd.exe token.
        /// </summary>
        public static string Quote(string value) {
            EnsureQuotable(value);
            return "\"" + value + "\"";
        }

        /// <summary>
        /// <paramref name="verbatim"/> followed by each argument as its own
        /// quoted token. Only <paramref name="verbatim"/> is trusted to carry
        /// syntax, so it must never be built from anything a user typed.
        /// </summary>
        public static string Build(string verbatim, params string[] arguments) {
            if (verbatim == null) {
                throw new ArgumentNullException("verbatim");
            }
            StringBuilder line = new StringBuilder(verbatim);
            if (arguments != null) {
                foreach (string argument in arguments) {
                    line.Append(' ').Append(Quote(argument));
                }
            }
            return line.ToString();
        }
    }
}
