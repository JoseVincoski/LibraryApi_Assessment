using LibrarySystem.Contracts.Common.Results;

namespace LibrarySystem.Service.Domain;

public sealed class Reader
{
    private Reader(Guid id, string name, string email)
    {
        Id = id;
        Name = name;
        Email = email;
    }

    public Guid Id { get; private set; }
    public string Name { get; private set; }
    public string Email { get; private set; }

    public static Result<Reader> Create(Guid? id, string name, string email)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return Result.Failure<Reader>(Error.Problem("Reader.EmptyName", "Name is required."));
        }

        if (string.IsNullOrWhiteSpace(email) || !email.Contains('@'))
        {
            return Result.Failure<Reader>(Error.Problem("Reader.InvalidEmail", "A valid email is required."));
        }

        return new Reader(id ?? Guid.NewGuid(), name.Trim(), email.Trim().ToLowerInvariant());
    }
}