using System.Data;
using System.Net.Mail;
using System.Runtime.InteropServices;
using System.Text;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using WMS.Application.Common.Interfaces;
using WMS.Domain.Constants;
using WMS.Domain.Entities;
using WMS.Infrastructure.Persistence.Contexts;
using WMS.Infrastructure.Security;

return await BootstrapAdminCommand.RunAsync(args);

internal static class BootstrapAdminCommand
{
    public static async Task<int> RunAsync(string[] args)
    {
        if (args.Length != 0 || Console.IsInputRedirected)
        {
            Console.Error.WriteLine("Run interactively with no arguments. Passwords are accepted only at the terminal prompt.");
            return 2;
        }

        var connectionString = Environment.GetEnvironmentVariable("ConnectionStrings__WmsConnection");
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            Console.Error.WriteLine("ConnectionStrings__WmsConnection must be set in the process environment.");
            return 2;
        }

        string email;
        string fullName;
        string passwordHash;
        try
        {
            email = ReadEmail();
            fullName = ReadFullName();

            var password = ReadPassword("Password: ");
            var confirmation = ReadPassword("Confirm password: ");
            if (!string.Equals(password, confirmation, StringComparison.Ordinal))
                throw new BootstrapInputException("Passwords do not match.");

            var passwordBytes = Encoding.UTF8.GetByteCount(password);
            if (string.IsNullOrWhiteSpace(password) || password.Length < 16 || passwordBytes > 72)
                throw new BootstrapInputException("Password must be at least 16 characters and at most 72 UTF-8 bytes.");

            passwordHash = new PasswordHasher().HashPassword(password);
        }
        catch (BootstrapInputException exception)
        {
            Console.Error.WriteLine(exception.Message);
            return 2;
        }
        catch (Exception)
        {
            Console.Error.WriteLine("Could not read or hash the credentials.");
            return 2;
        }

        try
        {
            var options = new DbContextOptionsBuilder<WmsDbContext>()
                .UseSqlServer(connectionString)
                .Options;
            await using var context = new WmsDbContext(options, new BootstrapTenantContext());
            await using var transaction = await context.Database.BeginTransactionAsync(IsolationLevel.Serializable);

            var systemAdminRoleId = await context.Roles.AsNoTracking()
                .Where(role => role.Name == Roles.SystemAdmin)
                .Select(role => (int?)role.Id)
                .SingleOrDefaultAsync();
            if (systemAdminRoleId is null)
            {
                Console.Error.WriteLine("SystemAdmin role was not found. Publish the DACPAC and its post-deployment script first.");
                return 1;
            }

            if (await context.UserRoles.AsNoTracking().AnyAsync(userRole => userRole.RoleId == systemAdminRoleId.Value))
            {
                Console.Error.WriteLine("A SystemAdmin already exists. Bootstrap is one-time only.");
                return 1;
            }

            if (await context.Users.AsNoTracking().AnyAsync(user => user.Email == email))
            {
                Console.Error.WriteLine("That email is already in use.");
                return 1;
            }

            var admin = User.Create(null, email, passwordHash, fullName);
            admin.AssignRole(systemAdminRoleId.Value, null);
            context.Users.Add(admin);
            await context.SaveChangesAsync();
            await transaction.CommitAsync();

            Console.WriteLine("SystemAdmin created successfully.");
            return 0;
        }
        catch (DbUpdateException exception) when (exception.InnerException is SqlException { Number: 2601 or 2627 })
        {
            Console.Error.WriteLine("A conflicting user or role assignment was inserted concurrently. Check the database before retrying.");
            return 1;
        }
        catch (SqlException exception) when (exception.Number == 1205)
        {
            Console.Error.WriteLine("Bootstrap lost a concurrent database transaction. Check for an existing SystemAdmin before retrying.");
            return 1;
        }
        catch (Exception)
        {
            Console.Error.WriteLine("Bootstrap failed. Check database access and the DACPAC deployment, then check whether a SystemAdmin exists before retrying.");
            return 1;
        }
    }

    private static string ReadEmail()
    {
        Console.Write("Email: ");
        var input = Console.ReadLine() ?? throw new BootstrapInputException("Email input was canceled.");
        var email = User.NormalizeEmail(input);
        if (email.Length == 0 || email.Length > 256 || email.Any(char.IsControl) ||
            !MailAddress.TryCreate(email, out var address) ||
            !string.Equals(address.Address, email, StringComparison.Ordinal))
            throw new BootstrapInputException("Enter a valid email of at most 256 characters.");
        return email;
    }

    private static string ReadFullName()
    {
        Console.Write("Full name: ");
        var fullName = Console.ReadLine()?.Trim() ?? throw new BootstrapInputException("Full name input was canceled.");
        if (fullName.Length == 0 || fullName.Length > 200 || fullName.Any(char.IsControl))
            throw new BootstrapInputException("Enter a full name of 1 to 200 characters.");
        return fullName;
    }

    private static string ReadPassword(string prompt)
    {
        Console.Write(prompt);
        var characters = new List<char>();
        try
        {
            while (true)
            {
                var key = Console.ReadKey(intercept: true);
                if (key.Key == ConsoleKey.Enter)
                    return new string(CollectionsMarshal.AsSpan(characters));
                if (key.Key == ConsoleKey.Backspace)
                {
                    if (characters.Count > 0)
                        characters.RemoveAt(characters.Count - 1);
                }
                else if (!char.IsControl(key.KeyChar))
                {
                    characters.Add(key.KeyChar);
                }
            }
        }
        finally
        {
            CollectionsMarshal.AsSpan(characters).Clear();
            Console.WriteLine();
        }
    }

    private sealed class BootstrapTenantContext : ITenantContext
    {
        public int? TenantId => null;
        public bool IsSystemRequest => true;
    }

    private sealed class BootstrapInputException(string message) : Exception(message);
}
