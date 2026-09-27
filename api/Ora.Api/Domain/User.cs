using Microsoft.AspNetCore.Identity;

namespace Ora.Api.Domain;

/// <summary>
/// A person who signs in to Ora.
/// </summary>
/// <remarks>
/// Identity owns every column; Ora adds nothing yet, but the subclass keeps a stable type to extend later.
/// </remarks>
public sealed class User : IdentityUser<Guid>;
