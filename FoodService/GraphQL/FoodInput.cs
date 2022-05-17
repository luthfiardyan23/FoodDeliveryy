namespace FoodService.GraphQL
{
    public record FoodInput
    (
        int? Id,
        string Name,
        int Stock,
        float Price
    );
}
