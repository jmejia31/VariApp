using System.Text.RegularExpressions;

namespace InventoryApp.Tests;

public sealed class MatrixGovernanceContractTests
{
    private static readonly Regex StableId = new(
        "^VAEP-MX::[A-Z0-9_]+::[A-Z0-9_]+$",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    [Fact]
    public void CatalogAndTemplate_EnforceStableMatrixGovernance()
    {
        var root = FindRepositoryRoot();
        var governanceRoot = Path.Combine(root, "docs", "matrices-evaluacion", "00_GOBERNANZA");
        var catalog = File.ReadAllText(Path.Combine(governanceRoot, "CATALOGO_MATRICES.md"));
        var template = File.ReadAllText(Path.Combine(governanceRoot, "PLANTILLA_MATRIZ_UI.md"));

        var rows = catalog
            .Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(line => line.StartsWith("| VAEP-MX::", StringComparison.Ordinal))
            .Select(ParseCatalogRow)
            .ToArray();

        Assert.Equal(49, rows.Length);
        Assert.Equal(rows.Length, rows.Select(row => row.MatrixId).Distinct(StringComparer.Ordinal).Count());
        Assert.All(rows, row => Assert.Matches(StableId, row.MatrixId));
        Assert.DoesNotContain(rows, row => Regex.IsMatch(
            row.MatrixId.Split("::", StringSplitOptions.None)[^1],
            "^(ROW|MATRIX|MX)[-_]?\\d+$",
            RegexOptions.IgnoreCase | RegexOptions.CultureInvariant));

        var ids = rows.Select(row => row.MatrixId).ToHashSet(StringComparer.Ordinal);
        Assert.All(rows, row =>
        {
            Assert.False(string.IsNullOrWhiteSpace(row.Domain));
            Assert.False(string.IsNullOrWhiteSpace(row.Kind));
            Assert.False(string.IsNullOrWhiteSpace(row.ImplementationRef));
            Assert.True(row.Parent == "ROOT" || ids.Contains(row.Parent), $"Unknown parent {row.Parent} for {row.MatrixId}");
            Assert.NotEqual("MATERIAL_WITHOUT_ID", row.Status);
        });

        Assert.Equal(rows.Length, rows.Select(row => row.ImplementationRef).Distinct(StringComparer.Ordinal).Count());
        Assert.Contains("Conteo exacto: **49 contract roots**", catalog, StringComparison.Ordinal);
        Assert.Contains("MATERIAL_WITHOUT_ID", catalog, StringComparison.Ordinal);

        var requiredTemplateTokens = new[]
        {
            "MATRIX_ID:", "MATRIX_CHANGE_ID:", "MATRIX_VERSION:", "PARENT_MATRIX_ID:", "CONTRACT_KIND:",
            "CONTRACT_OWNER:", "DATA_OWNER:", "DEPENDS_ON_MATRIX_IDS:",
            "DATA_ENTITIES:", "DB_TABLES:", "DB_FIELDS:", "MIGRATION_REFS:", "FK_CONSTRAINTS:",
            "INDEX_REFS:", "TRANSACTION_BOUNDARY:", "INTEGRITY_RULES:",
            "API_ROUTE:", "HTTP_METHOD:", "CONTROLLER_ACTION:", "REQUEST_DTO:", "APPLICATION_USE_CASE:",
            "REPOSITORY:", "BACKGROUND_JOB:", "INTEGRATION_PROVIDER:", "CONFIG_KEYS:",
            "PRIMARY_ROUTE_OR_SURFACE:", "COMPONENT_REFS:", "FORM_REFS:", "DIALOG_REFS:", "WIDGET_REFS:",
            "STATE_MODEL:", "INTERACTIONS:", "ACCESSIBILITY_CONTRACT:",
            "AUTHN_REQUIRED:", "AUTHZ_POLICY_OR_PERMISSION:", "RBAC_MODULE_ACTION:", "TENANT_SCOPE:",
            "AUDIT_EVENTS:", "PII_CLASSIFICATION:", "LOG_REDACTION:", "RATE_LIMIT_POLICY:", "OBSERVABILITY_SIGNALS:",
            "CI_RUN_REFS:", "RECEIPT_REF:", "REVIEW_FIRST:"
        };

        Assert.All(requiredTemplateTokens, token => Assert.Contains(token, template, StringComparison.Ordinal));
        Assert.Contains("jamás se deriva de fila, índice, orden visual", template, StringComparison.Ordinal);
    }

    private static CatalogRow ParseCatalogRow(string line)
    {
        var cells = line.Trim().Trim('|').Split('|').Select(cell => cell.Trim().Trim('`')).ToArray();
        Assert.Equal(7, cells.Length);
        return new CatalogRow(cells[0], cells[1], cells[2], cells[3], cells[4], cells[5]);
    }

    private static string FindRepositoryRoot()
    {
        var current = new DirectoryInfo(AppContext.BaseDirectory);
        while (current is not null)
        {
            if (File.Exists(Path.Combine(current.FullName, "docs", "VAEP_AUTHORITY.md")))
            {
                return current.FullName;
            }
            current = current.Parent;
        }

        throw new DirectoryNotFoundException("Could not locate repository root containing docs/VAEP_AUTHORITY.md.");
    }

    private sealed record CatalogRow(
        string MatrixId,
        string Domain,
        string Kind,
        string ImplementationRef,
        string Parent,
        string Status);
}
