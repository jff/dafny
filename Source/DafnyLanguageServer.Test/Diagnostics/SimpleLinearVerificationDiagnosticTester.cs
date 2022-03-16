﻿using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Dafny.LanguageServer.IntegrationTest.Extensions;
using Microsoft.Dafny.LanguageServer.IntegrationTest.Util;
using Microsoft.Dafny.LanguageServer.Workspace.Notifications;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;

namespace Microsoft.Dafny.LanguageServer.IntegrationTest.Diagnostics;

[TestClass]
public class SimpleLinearVerificationDiagnosticTester : LinearVerificationDiagnosticTester {
  private const int MaxTestExecutionTimeMs = 10000;

  [TestInitialize]
  public override Task SetUp() => base.SetUp();

  [TestMethod/*, Timeout(MaxTestExecutionTimeMs)*/]
  public async Task EnsureVerificationDiagnosticsAreWorking() {
    var codeAndTrace = @"
    : s  |  |  |  I  I  |  | :predicate Ok() {
    : s  |  |  |  I  I  |  | :  true
    : s  |  |  |  I  I  |  | :}
    :    |  |  |  I  I  |  | :
    : s  S [S][ ][I][I][S] | :method Test(x: bool) returns (i: int)
    : s  S [=][=][-][-][~] | :   ensures i == 2
    : s  S [S][ ][I][I][S] | :{
    : s  S [S][ ][I][I][S] | :  if x {
    : s  S [S][ ][I][I][S] | :    i := 2;
    : s  S [=][=][-][-][~] | :  } else {
    : s  S [S][ ]/!\[I][S] | :    i := 1;
    : s  S [S][ ][I][I][S] | :  }
    : s  S [S][ ][I][I][S] | :}
    :    |  |  |  I  I  |  | :    
    : s  |  |  |  I  I  |  | :predicate OkBis() {
    : s  |  |  |  I  I  |  | :  false
    : s  |  |  |  I  I  |  | :}".StripMargin();
    var code = ExtractCode(codeAndTrace);
    var documentItem = CreateTestDocument(code);
    await client.OpenDocumentAndWaitAsync(documentItem, CancellationToken);
    var traces1 = await GetAllLineVerificationDiagnostics(documentItem);
    ApplyChange(ref documentItem, new Range(10, 9, 10, 10), "/");
    var traces2 = await GetAllLineVerificationDiagnostics(documentItem);
    ApplyChange(ref documentItem, new Range(10, 9, 10, 10), "2");
    var traces3 = await GetAllLineVerificationDiagnostics(documentItem);

    var expected = RenderTrace(traces1.Concat(traces2).Concat(traces3).ToList(), code);
    AssertWithDiff.Equal(codeAndTrace, expected);

  }
}