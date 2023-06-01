using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Goatrello.Data;
using Goatrello.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.IdentityModel.Tokens;
using MongoDB.Driver;
using Goatrello.Services;
using AspNetCoreHero.ToastNotification.Abstractions;
using System.Reflection;
using Microsoft.EntityFrameworkCore.Storage;

namespace Goatrello.Controllers
{
	[Authorize]
	public class ListingsController : CognizantController
	{
		private readonly GoatrelloDataContext _context;
		//for toast notification dependency injection
		private readonly INotyfService _notyf;

		public ListingsController(GoatrelloDataContext context, INotyfService notyf)
		{
			_context = context;
			_notyf = notyf;
		}

		// GET: Listings
		public async Task<IActionResult> Index(int? BoardId, string searchList, string searchCard, string actionButton,
			string sortDirectionCheck, string sortFieldID, string sortDirection = "asc", string sortField = "Date Created",
			string filterOverdue = "", string hideCompleted = "", string filterTemplates = "")
		{

			//Redirect if no ID.
			if (BoardId == null || _context.Boards == null)
			{
				return RedirectToAction("Index", "Boards");
			}

			//Clear Cookies
			CookieHelper.CookieSet(HttpContext, ControllerName() + "URL", "", -1);

			//Set Return URL
			ViewDataReturnURL();

			//change filter button color
			ViewData["Filtering"] = "royal text-white";

			//List of sort options
			string[] sortOptions = new[] { "Date Created", "Card Title", "Due Date" };

			//check user permissions
			var user = await GoatAuthorize.CreateFromContext(HttpContext);

			var listings = _context.Listings
				.Include(l => l.Board)
				.GoatAuthorize(user, GoatAuthorizeType.Read)
				.Where(l => l.BoardId == BoardId.GetValueOrDefault());

			//Get cards
			var cards = _context.Cards
				.Include(c => c.Listing)
				.Include(c => c.Checklists).ThenInclude(c => c.ChecklistItems)  //retrieve all the properties for displaying badges on Listing index
				.Include(c => c.Links)
				.Include(c => c.Attachments).ThenInclude(c => c.ServerFile)
				.Include(c => c.Labels).ThenInclude(c => c.Labels)
				.Where(c => c.Listing.BoardId == BoardId.GetValueOrDefault())
				.AsNoTracking();

			var labels = _context.LabelListLabels.Include(l => l.Label);


			var templateListing = listings
				.Include(l => l.Cards)
				.Where(l => l.IsTemplate == true).FirstOrDefault();

			if (templateListing != null)
			{
				ViewData["Templates"] = templateListing.Cards;
			}
			else
			{
				ViewData["Templates"] = new HashSet<Card>();
			}

			//Hide Board Edit 
			if (!user.BoardAccess(BoardId, GoatAuthorizeType.Admin).Result)
			{
				ViewData["IsNotAdmin"] = "disabled=disabled hidden=hidden";
			}

			if (!user.BoardAccess(BoardId, GoatAuthorizeType.Write).Result)
			{
				ViewData["EditPermissions"] = "disabled=disabled hidden=hidden";
			}


			//Hide Archived. Make visible option later
			if (true)
			{
				cards = cards.Where(c => c.IsArchived == false);
				listings = listings.Where(l => l.IsArchived == false);
			}


			#region Filters            

			//Hide template listing

			//Swap to templates if toggled
			//Filter by Template
			if (filterTemplates != "")
			{
				filterTemplates = "checked=\"checked\"";
				listings = listings
					.Where(l => l.IsTemplate == true);
				ViewData["Filtering"] = "btn-warning";
				ViewData["TemplatePreserve"] = "disabled=disabled hidden=hidden";
			}
			else
			{
				listings = listings
					.Where(l => l.IsTemplate != true);
			}
			ViewData["filterTemplates"] = filterTemplates;



			//Search by List Title
			if (!searchList.IsNullOrEmpty())
			{
				listings = listings.Where(b => b.Title.ToUpper().Contains(searchList.ToUpper()));
				ViewData["Filtering"] = "btn-warning";
			}

			//Search by Card Name.
			if (!searchCard.IsNullOrEmpty())
			{
				cards = cards.Where(c => c.Title.ToUpper().Contains(searchCard.ToUpper()));
				ViewData["Filtering"] = "btn-warning";
			}

			//Filter by Overdue
			if (filterOverdue != "")
			{
				filterOverdue = "checked=\"checked\"";
				cards = cards.Where(c => !c.IsDueDateComplete && c.DueDate <= DateTime.Now);
				ViewData["Filtering"] = "btn-warning";
			}
			ViewData["filterOverdue"] = filterOverdue;
			//Filter by Uncomplete
			if (hideCompleted != "")
			{
				hideCompleted = "checked=\"checked\"";
				cards = cards.Where(c => !c.IsDueDateComplete);
				ViewData["Filtering"] = "btn-warning";
			}
			ViewData["hideCompleted"] = hideCompleted;

			#endregion

			#region Sort
			//Code for Sort Direction. By Yuhao.
			if (!String.IsNullOrEmpty(actionButton)) //Form Submitted!
			{
				if (sortOptions.Contains(actionButton))//Change of sort is requested
				{
					if (actionButton == sortField) //Reverse order on same field
					{
						sortDirection = sortDirection == "asc" ? "desc" : "asc";
					}
					sortField = actionButton;//Sort by the button clicked
				}
				else //Sort by the controls in the filter area
				{
					sortDirection = String.IsNullOrEmpty(sortDirectionCheck) ? "asc" : "desc";
					sortField = sortFieldID;
				}
			}

			//Sort Cards
			if (sortField == "Due Date")
			{
				cards = (sortDirection == "asc") ?
					cards = cards.OrderBy(c => c.Title) :
					cards = cards.OrderByDescending(c => c.Title);

				if (sortDirection != "asc")
				{
					ViewData["Filtering"] = "btn-warning";
				}
			}
			else if (sortField == "Due Date")
			{
				cards = (sortDirection == "asc") ?
					cards.OrderBy(c => c.DueDate.HasValue == false).ThenBy(c => c.DueDate) :
					cards.OrderBy(c => c.DueDate.HasValue == false).ThenByDescending(c => c.DueDate);

				ViewData["Filtering"] = "btn-warning";
			}
			else if (sortField == "Overdue")// Default sort by Listing title
			{
				cards = (sortDirection == "asc") ?
				cards.OrderBy(c => c.DueDate.HasValue).ThenBy(c => c.DueDate.HasValue || !c.IsDueDateComplete) :
				cards.OrderBy(c => c.DueDate.HasValue).ThenBy(c => c.DueDate.HasValue || !c.IsDueDateComplete);

				ViewData["Filtering"] = "btn-warning";
			}
			else // Default sort by Listing ID
			{
				cards = (sortDirection == "asc") ?
				cards = cards.OrderByDescending(c => c.Id) :
				cards = cards.OrderBy(c => c.Id);

				if (sortDirection != "asc")
				{
					ViewData["Filtering"] = "btn-warning";
				}
			}

			//save sort options for next sorting action
			ViewData["sortField"] = sortField;
			ViewData["sortDirection"] = sortDirection;

			//SelectList for Sorting Options
			ViewBag.sortFieldID = new SelectList(sortOptions, sortField.ToString());
			#endregion

			//put the board record to the viewbag for display on top of the page
			var board = _context.Boards
				.Include(b => b.Administrators)
				.Include(b => b.HiddenFrom)
				.Include(b => b.Members)
				.Include(b => b.Observers)
				.Include(b => b.Listings)
				.Where(b => b.Id == BoardId.GetValueOrDefault())
				.AsNoTracking()
				.FirstOrDefault();
			ViewBag.Board = board;

			ViewBag.Cards = cards;

			ViewBag.Labels = labels;

			return View(await listings.ToListAsync());
		}

		// GET: Listings/Create
		public IActionResult Create(int BoardId, string BoardTitle)
		{
			ViewDataReturnURL();

			ViewData["BoardTitle"] = BoardTitle;
			Listing l = new Listing()
			{
				BoardId = BoardId
			};
			return View(l);
		}

		// POST: Listings/Create
		// To protect from overposting attacks, enable the specific properties you want to bind to.
		// For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
		[HttpPost]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> Create([Bind("Id,BoardId,Title,IsPrivate,IsTemplate")] Listing listing)
		{
			//Get return URL
			ViewDataReturnURL();

			if (ModelState.IsValid)
			{
				try
				{

					listing.HiddenFrom = new UserList();
					listing.Observers = new UserList();

					_context.Add(listing);
					await _context.SaveChangesAsync();

					//notification for create
					if (listing.Title.Length > 10)
						_notyf.Custom($"List {listing.Title.Substring(0, 10)}... is created!", 5, "#602AC3", "fa fa-check");
					else
						_notyf.Custom($"List {listing.Title} is created!", 5, "#602AC3", "fa fa-check");

					return RedirectToAction("Index", new { listing.BoardId });

				}
				catch (RetryLimitExceededException)
				{
					_notyf.Error("Unable to save changes after multiple attempts. Try again, and if the problem persists please contact your system administrator.", 10);
				}
				catch
				{
					_notyf.Error("Unable to save changes. Try again, and if the problem persists please contact your system administrator.", 10);
				}
			}
			ViewData["BoardId"] = new SelectList(_context.Boards, "Id", "Title", listing.BoardId);
			return View(listing);
		}

		// GET: Listings/Edit/5
		public async Task<IActionResult> Edit(int? id)
		{
			//Get return URL
			ViewDataReturnURL();

			if (id == null || _context.Listings == null)
			{
				return NotFound();
			}

			var listing = await _context.Listings
				.Include(b => b.HiddenFrom)
				.ThenInclude(b => b.Users)
				.ThenInclude(b => b.User)
				.Include(b => b.Observers)
				.ThenInclude(b => b.Users)
				.ThenInclude(b => b.User)
				.FirstOrDefaultAsync(m => m.Id == id);
			if (listing == null)
			{
				return NotFound();
			}
			ViewData["BoardId"] = new SelectList(_context.Boards, "Id", "Title", listing.BoardId);
			ViewData["Users"] = new SelectList(_context.Users, "Id", "DisplayName");
			return View(listing);
		}

		// POST: Listings/Edit/5
		// To protect from overposting attacks, enable the specific properties you want to bind to.
		// For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
		[HttpPost]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> Edit(int id, [Bind("Id,BoardId,Title,IsPrivate,IsTemplate")] Listing listing)
		{
			//Get return URL
			ViewDataReturnURL();

			if (id != listing.Id)
			{
				return NotFound();
			}

			if (ModelState.IsValid)
			{
				try
				{
					_context.Update(listing);
					await _context.SaveChangesAsync();

					//notification for edit
					if (listing.Title.Length > 10)
						_notyf.Custom($"Card {listing.Title.Substring(0, 10)}... is updated!", 5, "#602AC3", "fa fa-refresh");
					else
						_notyf.Custom($"Card {listing.Title} is updated!", 5, "#602AC3", "fa fa-refresh");
				}
				catch (RetryLimitExceededException)
				{
					_notyf.Error("Unable to save changes after multiple attempts. Try again, and if the problem persists please contact your system administrator.", 10);
				}
				catch
				{
					_notyf.Error("Unable to save changes. Try again, and if the problem persists please contact your system administrator.", 10);
				}
				return RedirectToAction("Index", new { listing.BoardId });
			}
			ViewData["BoardId"] = new SelectList(_context.Boards, "Id", "Title", listing.BoardId);
			return View(listing);
		}

		// GET: Listings/Delete/5
		public async Task<IActionResult> Delete(int? id)
		{
			//Get return URL
			ViewDataReturnURL();

			if (id == null || _context.Listings == null)
			{
				return NotFound();
			}

			var listing = await _context.Listings
				.Include(l => l.Board)
				.FirstOrDefaultAsync(m => m.Id == id);
			if (listing == null)
			{
				return NotFound();
			}

			return View(listing);
		}

		// POST: Listings/Delete/5
		[HttpPost, ActionName("Delete")]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> DeleteConfirmed(int id)
		{
			//Get return URL
			ViewDataReturnURL();

			if (_context.Listings == null)
			{
				return Problem("Entity set 'GoatrelloDataContext.Listings'  is null.");
			}
			var listing = await _context.Listings.FindAsync(id);
			try
			{
				if (listing != null)
				{
					listing.IsArchived = true;
				}

				await _context.SaveChangesAsync();

				//notification for delete
				if (listing.Title.Length > 10)
					_notyf.Custom($"List {listing.Title.Substring(0, 10)}... has been archived!", 5, "#495867", "fa fa-trash");
				else
					_notyf.Custom($"List {listing.Title} has been archived!", 5, "#495867", "fa fa-trash");
			}
			catch (RetryLimitExceededException)
			{
				_notyf.Error("Unable to save changes after multiple attempts. Try again, and if the problem persists please contact your system administrator.", 10);
			}
			catch
			{
				_notyf.Error("Unable to save changes. Try again, and if the problem persists please contact your system administrator.", 10);
			}

			return RedirectToAction("Index", "Listings", new { BoardId = listing.BoardId });
		}

		//User Managment
		//User Managment
		public async Task<IActionResult> AddUser(int listingId, int userId, string userListSelect)
		{
			var listing = await _context.Listings
				.Include(b => b.HiddenFrom)
				.ThenInclude(b => b.Users)
				.ThenInclude(b => b.User)
				.Include(b => b.Observers)
				.ThenInclude(b => b.Users)
				.ThenInclude(b => b.User)
				.FirstOrDefaultAsync(b => b.Id == listingId);

			var targetList = userListSelect switch
			{
				"Observer" => listing.Observers,
				"Hidden" => listing.HiddenFrom,
				_ => throw new Exception($"Invalid list selection: {userListSelect}")
			};
			try
			{
				var addUser = new UserListUser { ListId = targetList.Id, UserId = userId };
				var removeUsers = listing.Observers.Users.Where(u => u.UserId == userId)
					.Concat(listing.HiddenFrom.Users.Where(u => u.UserId == userId));

				// raise error if the user is already on that list
				if (targetList.Users.FirstOrDefault(u => u.UserId == userId) != null)
					_notyf.Error($"User is already part of that list!", 5);
				//remove user from other lists and add to the new list
				else
				{
					_context.UserListUsers.RemoveRange(removeUsers);
					_context.UserListUsers.Add(addUser);
					await _context.SaveChangesAsync();
					_notyf.Success("User successfully moved!", 5);
				}
			}
			catch (RetryLimitExceededException)
			{
				_notyf.Error("Unable to save changes after multiple attempts. Try again, and if the problem persists please contact your system administrator.", 10);
			}
			catch
			{
				_notyf.Error("Unable to save changes. Try again, and if the problem persists please contact your system administrator.", 10);
			}
			ViewData["Users"] = new SelectList(_context.Users, "Id", "DisplayName");

			return RedirectToAction("Edit", new { Id = listingId });
		}

		public async Task<IActionResult> RemoveUser(int listingId, int userId, string userListSelect)
		{
			var listing = await _context.Listings
				.Include(b => b.HiddenFrom)
				.ThenInclude(b => b.Users)
				.ThenInclude(b => b.User)
				.Include(b => b.Observers)
				.ThenInclude(b => b.Users)
				.ThenInclude(b => b.User)
				.FirstOrDefaultAsync(b => b.Id == listingId);

			var user = _context.UserListUsers.Where(u => u.UserId == userId);
			try
			{
				if (user != null)
				{
					switch (userListSelect)
					{
						case "Hidden":
							listing.HiddenFrom.Users.Remove(await user.Where(u => u.ListId == listing.HiddenFrom.Id).FirstOrDefaultAsync());
							break;
						case "Observer":
							listing.Observers.Users.Remove(await user.Where(u => u.ListId == listing.Observers.Id).FirstOrDefaultAsync());
							break;
						default: throw new Exception("User type not selected properly.");
					}
				}
				else
				{
					return NotFound();
				}

				await _context.SaveChangesAsync();
			}
			catch (RetryLimitExceededException)
			{
				_notyf.Error("Unable to save changes after multiple attempts. Try again, and if the problem persists please contact your system administrator.", 10);
			}
			catch
			{
				_notyf.Error("Unable to save changes. Try again, and if the problem persists please contact your system administrator.", 10);
			}

			ViewData["Users"] = new SelectList(_context.Users, "Id", "DisplayName");

			return RedirectToAction("Edit", new { Id = listingId });
		}


		private bool ListingExists(int id)
		{
			return (_context.Listings?.Any(e => e.Id == id)).GetValueOrDefault();
		}
		private void ViewDataReturnURL()
		{
			ViewData["returnURL"] = MaintainURL.ReturnURL(HttpContext, ControllerName());
		}
	}
}
