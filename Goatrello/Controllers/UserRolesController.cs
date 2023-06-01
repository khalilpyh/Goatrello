using Goatrello.Data;
using Goatrello.Models;
using Goatrello.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Goatrello.Services;
using AspNetCoreHero.ToastNotification.Abstractions;

namespace Goatrello.Controllers
{
	[Authorize]
	public class UserRolesController : Controller
	{
		private readonly GoatrelloDataContext _context;
		private readonly UserManager<User> _userManager;
		//for toast notification dependency injection
		private readonly INotyfService _notyf;

		public UserRolesController(GoatrelloDataContext context, UserManager<User> userManager, INotyfService notyf)
		{
			_context = context;
			_userManager = userManager;
			_notyf = notyf;
		}

		// GET: User
		public async Task<IActionResult> Index()
		{

			if (!PermissionCheck(out IActionResult redirect))
			{
				return redirect;
			}

			var users = await (from u in _context.Users
							   .OrderBy(u => u.UserName)
							   select new UserVM
							   {
								   Id = u.Id,
								   UserName = u.UserName,
								   DisplayName = u.DisplayName,
								   IsAdmin = u.IsSiteAdmin,
								   IsArchived = u.IsArchived,
							   }).ToListAsync();

			return View(users);
		}

		// GET: Checklists/ToggleArchive
		public async Task<IActionResult> ToggleArchive(int? id)
		{

			if (id == null)
			{
				return NotFound();
			}
			try
			{
				if (!PermissionCheck((int)id, "Archived", out IActionResult redirect))
				{
					return redirect;
				}

				var user = await _context.Users.FindAsync(id);

				if (user != null)
				{
					user.IsArchived = !user.IsArchived;
					await _context.SaveChangesAsync();
				}
			}
			catch
			{
				_notyf.Error("Unable to change status. Please try again.", 5);
			}

			return RedirectToAction(nameof(Index));
		}

		// GET: Checklists/ToggleArchive
		public async Task<IActionResult> ToggleAdmin(int? id)
		{
			if (id == null)
			{
				return NotFound();
			}

			try
			{
				if (!PermissionCheck((int)id, "Admin", out IActionResult redirect))
				{
					return redirect;
				}

				var user = await _context.Users.FindAsync(id);

				if (user != null)
				{
					user.IsSiteAdmin = !user.IsSiteAdmin;
					await _context.SaveChangesAsync();
				}
			}
			catch
			{
				_notyf.Error("Unable to change status. Please try again.", 5);
			}

			return RedirectToAction(nameof(Index));
		}

		/// <summary>
		/// Method for checking user's permissions. 
		/// Nest inside of an if statement and use out parameter as redirect target for if check fails.
		/// </summary>
		/// <param name="redirect">Out Parameter for redirecting if user lacks permissions.</param>
		/// <returns></returns>
		public bool PermissionCheck(out IActionResult redirect)
		{
			redirect = RedirectToAction("Index", "Home");

			try
			{
				var currentUser = GoatAuthorize.CreateFromContext(HttpContext).Result;

				if (!currentUser.IsSiteAdmin || currentUser.IsArchived)
				{
					//Return true if user is archived or not admin
					_notyf.Error($"You do not have permission to view that page.", 5);
					return false;
				}

				//Return true if checks are passed
				return true;
			}
			catch
			{
				//Return false in event that user is unable to be varified.
				_notyf.Error($"There was an error checking your permissions. Pleasure ensure you are properly logged in and have not timed out.", 5);
				return false;
			}
		}
		/// <summary>
		/// Method for checking user's permissions. 
		/// Nest inside of an if statement and use out parameter as redirect target for if check fails.
		/// Has ID and ActionType inputs for checking if current user is not trying to edit their own status.
		/// </summary>
		/// <param name="id">ID of target user what current user is attempting to change status of.</param>
		/// <param name="actionType">The type of status that is being changed, such as "Admin" status, or "Archived" status.</param>
		/// <param name="redirect">Out Parameter for redirecting if user lacks permissions.</param>
		/// <returns></returns>
		public bool PermissionCheck(int id, string actionType, out IActionResult redirect)
		{
			redirect = RedirectToAction("Index", "Home");


			try
			{
				var currentUser = GoatAuthorize.CreateFromContext(HttpContext).Result;

				if (!currentUser.IsSiteAdmin || currentUser.IsArchived)
				{
					//Return true if user is archived or not admin
					_notyf.Error($"You do not have permission to view that page.", 5);
					return false;
				}
				else if (User.GetUserId() == id)
				{
					_notyf.Error($"You cannot change your own {actionType} status.", 5);
					redirect = RedirectToAction(nameof(Index));
					return false;
				}

				//Return true if checks are passed
				return true;
			}
			catch
			{
				//Return false in event that user is unable to be varified.
				_notyf.Error($"There was an error checking your permissions. Pleasure ensure you are properly logged in and have not timed out.", 5);
				return false;
			}
		}

	}
}
