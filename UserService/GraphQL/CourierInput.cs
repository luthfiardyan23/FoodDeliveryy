namespace UserService.GraphQL
{
    public record CourierInput
    (
        int? Id,
        string CourierName,
        string Phone,
        int userId
    );
}
