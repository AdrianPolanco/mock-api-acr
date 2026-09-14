namespace MockApi.Api.Entities;

/// <summary>
/// EF Core entity persisted in the in-memory database. Deliberately simple —
/// this API exists as a containerizable artifact for the AI-200 labs
/// (01-containers: ACR, App Service, Container Apps, AKS), not as production code.
/// </summary>
public class Product
{
    public int Id { get; set; }

    public required string Name { get; set; }

    public decimal Price { get; set; }

    public int Stock { get; set; }

    public DateTime CreatedAtUtc { get; set; }
}
