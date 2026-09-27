using Ora.Api.Domain;

namespace Ora.Api.GraphQL.Users;

/// <summary>
/// Exposes only the safe parts of an Identity user; password hashes and security stamps never reach the schema.
/// </summary>
[ObjectType<User>]
public static partial class UserNode
{
    static partial void Configure(IObjectTypeDescriptor<User> descriptor)
    {
        descriptor.BindFieldsExplicitly();
        descriptor.Field(u => u.Email).Type<NonNullType<StringType>>();
    }

    [NodeResolver]
    public static async Task<User?> GetUserByIdAsync(
        Guid id,
        IVisibleUserByIdDataLoader userById,
        CancellationToken cancellationToken) =>
        await userById.LoadAsync(id, cancellationToken);
}
