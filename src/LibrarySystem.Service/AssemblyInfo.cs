using System.Runtime.CompilerServices;

// Lets the Testcontainers-backed test projects reference internal constants
// like LoanConfiguration.ActiveLoanPerBookIndexName instead of duplicating them.
[assembly: InternalsVisibleTo("LibrarySystem.IntegrationTests")]
[assembly: InternalsVisibleTo("LibrarySystem.SystemTests")]
