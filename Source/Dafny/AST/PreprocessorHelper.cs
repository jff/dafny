﻿using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.IO;
using System.Text;

namespace Microsoft.Dafny; 

// Same as Boogie.ParserHelper, except that it takes newlines into account
public static class PreprocessorHelper {
  struct ReadState {
    public bool hasSeenElse;
    public bool mayStillIncludeAnotherAlternative;

    public ReadState(bool hasSeenElse, bool mayStillIncludeAnotherAlternative) {
      this.hasSeenElse = hasSeenElse;
      this.mayStillIncludeAnotherAlternative = mayStillIncludeAnotherAlternative;
    }
  }

  // "arg" is assumed to be trimmed
  private static bool IfdefConditionSaysToInclude(string arg, List<string> /*!*/ defines) {
    Contract.Requires(arg != null);
    Contract.Requires(cce.NonNullElements(defines));
    bool sense = true;
    while (arg.StartsWith("!")) {
      sense = !sense;
      arg = arg.Substring(1).TrimStart();
    }

    return defines.Contains(arg) == sense;
  }

  public static string Fill(Stream stream, List<string> /*!*/ defines, string newline) {
    Contract.Requires(stream != null);
    Contract.Requires(cce.NonNullElements(defines));
    Contract.Ensures(Contract.Result<string>() != null);
    StreamReader /*!*/
      reader = new StreamReader(stream);
    var o = stream.CanSeek;
    return Fill(reader, defines, newline);
  }

  public static string Fill(TextReader reader, List<string> /*!*/ defines, string newline) {
    Contract.Requires(reader != null);
    Contract.Requires(cce.NonNullElements(defines));
    Contract.Ensures(Contract.Result<string>() != null);
    StringBuilder sb = new StringBuilder();
    List<ReadState> /*!*/
      readState = new List<ReadState>(); // readState.Count is the current nesting level of #if's
    int ignoreCutoff =
      -1; // -1 means we're not ignoring; for 0<=n, n means we're ignoring because of something at nesting level n
    while (true)
    //invariant -1 <= ignoreCutoff && ignoreCutoff < readState.Count;
    {
      string s = reader.ReadLine();
      if (s == null) {
        if (readState.Count != 0) {
          sb.AppendLine("#MalformedInput: missing #endif");
        }

        break;
      }

      string t = s.Trim();
      if (t.StartsWith("#if")) {
        ReadState rs = new ReadState(false, false);
        if (ignoreCutoff != -1) {
          // we're already in a state of ignoring, so continue to ignore
        } else if (IfdefConditionSaysToInclude(t.Substring(3).TrimStart(), defines)) {
          // include this branch
        } else {
          ignoreCutoff = readState.Count; // start ignoring
          rs.mayStillIncludeAnotherAlternative = true; // allow some later "elsif" or "else" branch to be included
        }

        readState.Add(rs);
        sb.Append(newline); // ignore the #if line
      } else if (t.StartsWith("#elsif")) {
        ReadState rs;
        if (readState.Count == 0 || (rs = readState[readState.Count - 1]).hasSeenElse) {
          sb.Append("#MalformedInput: misplaced #elsif" + newline); // malformed input
          break;
        }

        if (ignoreCutoff == -1) {
          // we had included the previous branch
          //Contract.Assert(!rs.mayStillIncludeAnotherAlternative);
          ignoreCutoff = readState.Count - 1; // start ignoring
        } else if (rs.mayStillIncludeAnotherAlternative &&
                   IfdefConditionSaysToInclude(t.Substring(6).TrimStart(), defines)) {
          // include this branch, but no subsequent branch at this level
          ignoreCutoff = -1;
          rs.mayStillIncludeAnotherAlternative = false;
          readState[readState.Count - 1] = rs;
        }

        sb.Append(newline); // ignore the #elsif line
      } else if (t == "#else") {
        ReadState rs;
        if (readState.Count == 0 || (rs = readState[readState.Count - 1]).hasSeenElse) {
          sb.Append("#MalformedInput: misplaced #else" + newline); // malformed input
          break;
        }

        rs.hasSeenElse = true;
        if (ignoreCutoff == -1) {
          // we had included the previous branch
          //Contract.Assert(!rs.mayStillIncludeAnotherAlternative);
          ignoreCutoff = readState.Count - 1; // start ignoring
        } else if (rs.mayStillIncludeAnotherAlternative) {
          // include this branch
          ignoreCutoff = -1;
          rs.mayStillIncludeAnotherAlternative = false;
        }

        readState[readState.Count - 1] = rs;
        sb.Append(newline); // ignore the #else line
      } else if (t == "#endif") {
        if (readState.Count == 0) {
          sb.Append("#MalformedInput: misplaced #endif" + newline); // malformed input
          break;
        }

        readState.RemoveAt(readState.Count - 1); // pop
        if (ignoreCutoff == readState.Count) {
          // we had ignored the branch that ends here; so, now we start including again
          ignoreCutoff = -1;
        }

        sb.Append(newline); // ignore the #endif line
      } else if (ignoreCutoff == -1) {
        sb.Append(s);
        sb.Append(newline);
      } else {
        sb.Append(newline); // ignore the line
      }
    }

    return sb.ToString();
  }

  public static string DetectNewline(string filename) {
    string newline;
    using StreamReader reader = new StreamReader(filename);
    var newlineDetector = new char[2] { '\0', '\0' };
    var wasCr = 0;
    while (!reader.EndOfStream) {
      reader.ReadBlock(newlineDetector, wasCr, 1);
      if (wasCr > 0 || newlineDetector[0] == '\n') {
        break;
      }

      if (newlineDetector[0] == '\r') {
        wasCr++;
      }
    }

    if (wasCr == 1) {
      if (newlineDetector[1] == '\n') {
        newline = "\r\n";
      } else {
        newline = "\r";
      }
    } else {
      newline = "\n";
    }

    return newline;
  }
}