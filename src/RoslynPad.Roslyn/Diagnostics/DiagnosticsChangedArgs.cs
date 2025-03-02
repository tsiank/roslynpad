using Microsoft.CodeAnalysis;

namespace RoslynPad.Roslyn.Diagnostics;

public record DiagnosticsChangedArgs(DocumentId DocumentId, ISet<DiagnosticData> AddedDiagnostics, ISet<DiagnosticData> RemovedDiagnostics);
