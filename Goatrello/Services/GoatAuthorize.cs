using System.Diagnostics;
using System.Security.Claims;
using Goatrello.Data;
using Goatrello.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.EntityFrameworkCore;


namespace Goatrello.Services;

/// <summary>
/// Represents the different permission levels a user can have on a board or listing
/// </summary>
public enum GoatAuthorizeType
{
    Admin,
    Write,
    Read
}

/// <summary>
/// Extensions to simplify limiting access to content based on user permissions
/// </summary>
public static class AuthorizationExtensions
{
    /// <summary>
    /// Quickly grab the userID of the logged in user
    /// </summary>
    public static int GetUserId(this ClaimsPrincipal principal)
    {
        if (principal == null)
            throw new ArgumentNullException(nameof(principal));
        string id = principal.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return (id == null) ? 0 : int.Parse(id);
    }

    /// <summary>
    /// Extension method to filter db queries to only boards that user has permission to access
    /// </summary>
    public static IQueryable<Board> GoatAuthorize(this IQueryable<Board> board, GoatAuthorize user,
        GoatAuthorizeType request)
    {
        if (user.IsArchived)
            //issue is Enumerable doesn't mock the async code! 
            return board.Where(b => false).AsQueryable();
        if (user.IsSiteAdmin)
            //site admins have access to all content without restriction
            return board.AsQueryable();
        //include additional fields in the query
        board = board
            .Include(b => b.Administrators)
            .ThenInclude(a => a.Users)
            .Include(b => b.HiddenFrom)
            .ThenInclude(a => a.Users)
            .Include(b => b.Members)
            .ThenInclude(a => a.Users)
            .Include(b => b.Observers)
            .ThenInclude(a => a.Users)
            .AsQueryable();
        
        switch (request)
        {
            case GoatAuthorizeType.Admin:
                //only board admins can admin boards
                return board
                    .Where(b => b.Administrators.Users.Any(u => u.UserId == user.Id))
                    .AsQueryable();
            case GoatAuthorizeType.Read:
                //board admin can always read.
                //public: if not hidden from, can read
                //private: members, observers can read 
                return board
                    .Where(b => b.Administrators.Users.Any(u => u.UserId == user.Id) 
                        || (b.IsPrivate 
                            && (b.Observers.Users.Any(u => u.UserId == user.Id)
                                || b.Members.Users.Any(u => u.UserId == user.Id)))
                        || (!b.IsPrivate
                            && b.HiddenFrom.Users.All(u => u.UserId != user.Id)))
                    .AsQueryable();
            case GoatAuthorizeType.Write:
                //only admin and members can write (ie. add a new list, add to list)
                return board
                    .Where(b => b.Members.Users.Any(u => u.UserId == user.Id)
                        || b.Administrators.Users.Any(u => u.UserId == user.Id))
                    .AsQueryable();
            default:
                return board.Where(b => false).AsQueryable();
        }
    }

    /// <summary>
    /// Extension method to filter db queries to only listings that user has permission to access
    /// </summary>
    public static IQueryable<Listing> GoatAuthorize(this IQueryable<Listing> listing, GoatAuthorize user,
        GoatAuthorizeType request)
    {
        if (user.IsArchived)
            return listing.Where(b => false).AsQueryable();
        if (user.IsSiteAdmin)
            //site admins have access to all content without restriction
            return listing.AsQueryable();
        //include additional fields in the query
        listing = listing
            .Include(l => l.Observers)
            .ThenInclude(a => a.Users)
            .Include(l => l.HiddenFrom)
            .ThenInclude(a => a.Users)
            .Include(l => l.Board)
            .ThenInclude(b => b.Administrators)
            .ThenInclude(a => a.Users)
            .Include(l => l.Board)
            .ThenInclude(b => b.HiddenFrom)
            .ThenInclude(a => a.Users)
            .Include(l => l.Board)
            .ThenInclude(b => b.Members)
            .ThenInclude(a => a.Users)
            .Include(l => l.Board)
            .ThenInclude(b => b.Observers)
            .ThenInclude(a => a.Users)
            .AsQueryable();
        
        switch (request)
        {
            case GoatAuthorizeType.Read:
            //board admin can always read.
            //public: if not hidden from, and board is public, and board not hidden from, can read
            //private: observers, and can read board, can read
            return listing
                .Where(l => l.Board.Administrators.Users.Any(u => u.UserId == user.Id) 
                    || (l.IsPrivate 
                        && (l.Observers.Users.Any(u => u.UserId == user.Id)
                            && (l.Board.Observers.Users.Any(u => u.UserId == user.Id)
                            || l.Board.Members.Users.Any(u => u.UserId == user.Id))))
                    || (!l.IsPrivate 
                        && l.Board.IsPrivate
                        && (l.HiddenFrom.Users.All(u => u.UserId != user.Id)
                            && (l.Board.Observers.Users.Any(u => u.UserId == user.Id)
                                || l.Board.Members.Users.Any(u => u.UserId == user.Id))))
                   || (!l.IsPrivate 
                        && !l.Board.IsPrivate
                        && l.HiddenFrom.Users.All(u => u.UserId != user.Id) 
                        && (l.Board.HiddenFrom.Users.All(u => u.UserId != user.Id))))
                .AsQueryable();
            case GoatAuthorizeType.Write:
                throw new NotImplementedException("Write Permissions not setup as a listing filter");
            case GoatAuthorizeType.Admin:
                throw new NotImplementedException("Write Permissions not setup as a listing filter");
            default:
                return listing.Where(b => false).AsQueryable();
        }
    }
}


/// <summary>
/// Represents a logged in user and their top level permissions. Allows you to query additional permissions
/// </summary>
public struct GoatAuthorize
{
    public readonly int Id;
    public readonly bool IsSiteAdmin;
    public readonly bool IsArchived;
    private readonly GoatrelloDataContext _context;

    /// <summary>
    /// performs a query and returns a GoatAuthorize object that represents the logged in user permissions
    /// </summary>
    /// <param name="context"></param>
    /// <returns></returns>
    public static async Task<GoatAuthorize> CreateFromContext(HttpContext context)
    {
        var user = await context.RequestServices.GetService<GoatrelloDataContext>().Users
            .Where(u => u.Id == context.User.GetUserId())
            .Select(u => new { u.Id, u.IsSiteAdmin, u.IsArchived })
            .FirstOrDefaultAsync();
        return new GoatAuthorize(user.Id, user.IsSiteAdmin, user.IsArchived, context.RequestServices.GetService<GoatrelloDataContext>());
    }

    /// <summary>
    /// internal method to generate a user object
    /// </summary>
    private GoatAuthorize(int userId, bool userIsSiteAdmin, bool userIsArchived, GoatrelloDataContext context)
    {
        Id = userId;
        IsSiteAdmin = userIsSiteAdmin;
        IsArchived = userIsArchived;
        _context = context;
    }

    /// <summary>
    /// Makes a db query and returns true if the logged in user should have the requested permission to access the board.
    /// </summary>
    public async Task<bool> BoardAccess(int? boardId, GoatAuthorizeType request)
    {
        if (IsSiteAdmin)
            return true;
        if (IsArchived)
            return false;
        int userId = this.Id;
        switch (request)
        {
            case GoatAuthorizeType.Admin:
                return await this._context.Boards
                    .Include(b => b.Administrators)
                    .ThenInclude(a => a.Users)
                    .Where(b => b.Id == boardId
                        && b.Administrators.Users.Any(u => u.UserId == userId))
                    .AnyAsync();
            case GoatAuthorizeType.Write:
                return await this._context.Boards
                    .Include(b => b.Administrators)
                    .ThenInclude(a => a.Users)
                    .Include(b => b.Members)
                    .ThenInclude(m => m.Users)
                    .Where(b => b.Id == boardId
                        && (b.Administrators.Users.Any(u => u.UserId == userId) 
                            || b.Members.Users.Any(u => u.UserId == userId)))
                    .AnyAsync();
            case GoatAuthorizeType.Read:
                return (await this._context.Boards
                    .Include(b => b.HiddenFrom)
                    .ThenInclude(u => u.Users)
                    .Where(b => b.Id == boardId
                        && !b.IsPrivate
                        && b.HiddenFrom.Users.All(u => u.UserId != userId))
                    .AnyAsync() 
                    || await this._context.Boards
                    .Include(b => b.Administrators)
                    .ThenInclude(a => a.Users)
                    .Include(b => b.Members)
                    .ThenInclude(m => m.Users)
                    .Include(b => b.Observers)
                    .ThenInclude(o => o.Users)
                    .Where(b => b.Id == boardId
                        && (b.Administrators.Users.Any(u => u.UserId == userId) 
                            || b.Members.Users.Any(u => u.UserId == userId))
                            || b.Observers.Users.Any(u => u.UserId == userId))
                    .AnyAsync());
            default: return false;
        }
    }
    
    /// <summary>
    /// Makes a db query and returns true if the logged in user should have the requested permission to access the listing.
    /// </summary>
    public async Task<bool> ListingAccess(int? listingId, GoatAuthorizeType request)
    {
        if (IsSiteAdmin)
            return true;
        if (IsArchived)
            return false;
        int userId = this.Id;
        switch (request)
        {
            case GoatAuthorizeType.Write:
                return await this._context.Listings
                    .Include(l => l.Board)
                    .ThenInclude(b => b.Administrators)
                    .ThenInclude(a => a.Users)
                    .Include(l => l.Board)
                    .ThenInclude(b => b.Members)
                    .ThenInclude(a => a.Users)
                    .Where(l => l.Id == listingId
                        && (l.Board.Administrators.Users.Any(u => u.UserId == userId)
                            || (l.Board.Members.Users.Any(u => u.UserId == userId)
                                && ((l.HiddenFrom.Users.All(u => u.UserId != userId)
                                    && l.IsPrivate == false)
                                    || (l.Observers.Users.Any(u => u.UserId == userId)
                                        && l.IsPrivate == true)))))
                    .AnyAsync();
            case GoatAuthorizeType.Read:
                return (await this._context.Listings
                    .Include(l => l.Board)
                    .ThenInclude(b => b.HiddenFrom)
                    .ThenInclude(u => u.Users)
                    .Where(l => l.Id == listingId
                                && l.IsPrivate == false
                                && l.HiddenFrom.Users.All(u => u.UserId != userId)
                                && l.Board.IsPrivate == false
                                && l.Board.HiddenFrom.Users.All(u => u.UserId != userId))
                    .AnyAsync()
                    || await this._context.Listings
                    .Include(l => l.Board)
                    .ThenInclude(b => b.Administrators)
                    .ThenInclude(a => a.Users)
                    .Include(l => l.Board)
                    .ThenInclude(b => b.Members)
                    .ThenInclude(a => a.Users)
                    .Include(l => l.Board)
                    .ThenInclude(b => b.Observers)
                    .ThenInclude(a => a.Users)
                    .Include(l => l.Board)
                    .ThenInclude(b => b.HiddenFrom)
                    .ThenInclude(a => a.Users)
                    .Where(l => l.Id == listingId
                        && (l.Board.Administrators.Users.Any(u => u.UserId == userId)
                            || (l.Board.Members.Users.Any(u => u.UserId == userId)
                                && ((l.HiddenFrom.Users.All(u => u.UserId != userId)
                                     && l.IsPrivate == false)
                                    || (l.Observers.Users.Any(u => u.UserId == userId)
                                        && l.IsPrivate == true)))))
                    .AnyAsync());
            default: return false;
        }
    }

}