using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Goatrello.Data;
using Goatrello.Models;
using Goatrello.Services;
using AspNetCoreHero.ToastNotification.Abstractions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.IdentityModel.Tokens;

namespace Goatrello.Controllers
{
    [Authorize]
    public class ChecklistsController : CognizantController
    {
        private readonly GoatrelloDataContext _context;
		//for toast notification dependency injection
		private readonly INotyfService _notyf;

		public ChecklistsController(GoatrelloDataContext context, INotyfService notyf)
        {
            _context = context;
			_notyf = notyf;
		}
        // GET: Checklists/Create
        public IActionResult Create(int CardId)
        {
            ViewDataReturnURL(CardId);
			Checklist c = new Checklist()
            {
                CardId = CardId
            };


            return View(c);
        }

        // POST: Checklists/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Id,CardId,Title,IsShowHidden,IsArchived")] Checklist checklist)
        {

            if (ModelState.IsValid)
            {
                _context.Add(checklist);
                AddActionActivity(checklist.CardId, $"added checklist {checklist.Title} to this card.");
                await _context.SaveChangesAsync();

				//notification for create
				if (checklist.Title.Length > 10)
					_notyf.Custom($"Checklist {checklist.Title.Substring(0, 10)}... is added!", 5, "#602AC3", "fa fa-check");
				else
					_notyf.Custom($"Checklist {checklist.Title} is added!", 5, "#602AC3", "fa fa-check");
			}
            ViewData["CardId"] = new SelectList(_context.Cards, "Id", "Title", checklist.CardId);

            return RedirectToAction("Details", "Cards", new { Id = checklist.CardId });
        }

        // GET: Checklists/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {

            if (id == null || _context.Checklists == null)
            {
                return NotFound();
            }

            var checklist = await _context.Checklists
                .Include(c => c.Card)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (checklist == null)
            {
                return NotFound();
            }

            ViewDataReturnURL(checklist.CardId);

            ViewData["CardId"] = new SelectList(_context.Cards, "Id", "Title", checklist.CardId);
            return View(checklist);
        }

        // POST: Checklists/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,CardId,Title,IsShowHidden,IsArchived")] Checklist checklist)
        {
            if (id != checklist.Id)
            {
                return NotFound();
            }

            if (checklist.Title.IsNullOrEmpty())
            {
                _notyf.Error($"Invalid checklist title.", 5);
                return RedirectToAction("Edit", new { Id = id });
            }
            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(checklist);
                    await _context.SaveChangesAsync();

					//notification for edit
					if (checklist.Title.Length > 10)
						_notyf.Custom($"Checklist {checklist.Title.Substring(0, 10)}... is updated!", 5, "#602AC3", "fa fa-refresh");
					else
						_notyf.Custom($"Checklist {checklist.Title} is updated!", 5, "#602AC3", "fa fa-refresh");
				}
                catch (DbUpdateConcurrencyException)
                {
                    if (!ChecklistExists(checklist.Id))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
            }
            ViewData["CardId"] = new SelectList(_context.Cards, "Id", "Title", checklist.CardId);

            return RedirectToAction("Details", "Cards", new { Id = checklist.CardId });
        }

        // GET: Checklists/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null || _context.Checklists == null)
            {
                return NotFound();
            }

            var checklist = await _context.Checklists
                .Include(c => c.Card)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (checklist == null)
            {
                return NotFound();
            }

            ViewDataReturnURL(checklist.CardId);

            return View(checklist);
        }

        // POST: Checklists/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            if (_context.Checklists == null)
            {
                return Problem("Entity set 'GoatrelloDataContext.Checklists'  is null.");
            }
            var checklist = await _context.Checklists.FindAsync(id);
            if (checklist != null)
            {
                checklist.IsArchived= true;
                AddActionActivity(checklist.CardId, $"archived checklist {checklist.Title}.");
                await _context.SaveChangesAsync();

				//notification for delete
				if (checklist.Title.Length > 10)
					_notyf.Custom($"Checklist {checklist.Title.Substring(0, 10)}... has been removed!", 5, "#495867", "fa fa-trash");
				else
					_notyf.Custom($"Checklist {checklist.Title} has been removed!", 5, "#495867", "fa fa-trash");
			}

            return RedirectToAction("Details", "Cards", new { Id = checklist.CardId });
        }


        // GET: Checklists/AddItem
        public IActionResult AddItem(int ChecklistId, int CardId)
        {
            ViewDataReturnURL(CardId);

            ViewData["CardId"] = CardId;


            ChecklistItem c = new ChecklistItem()
            {
                ChecklistId = ChecklistId
            };

            return View(c);
        }

        // POST: Checklists/AddItem
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddItem([Bind("Id,ChecklistId,Item,IsChecked,IsArchived")] ChecklistItem checklistitem)
        {

            if (ModelState.IsValid)
            {
                _context.ChecklistItems.Add(checklistitem);
                await _context.SaveChangesAsync();

				//notification for add
				if (checklistitem.Item.Length > 10)
					_notyf.Custom($"Item {checklistitem.Item.Substring(0, 10)}... has been added to the checklist!", 5, "#602AC3", "fa fa-check");
				else
					_notyf.Custom($"Item {checklistitem.Item} has been added to the checklist!", 5, "#602AC3", "fa fa-check");
			}

            return ReturnToCardByChecklist(checklistitem.ChecklistId);
        }

		// GET: Checklists/EditItem/5
		public async Task<IActionResult> EditItem(int? id)
		{
			if (id == null || _context.Checklists == null)
			{
				return NotFound();
			}

			var checklistitem = await _context.ChecklistItems
				.Include(c => c.Checklist).ThenInclude(c => c.Card)
				.FirstOrDefaultAsync(c => c.Id == id);

			if (checklistitem == null)
			{
				return NotFound();
			}

            ViewDataReturnURL(checklistitem.Checklist.CardId);

            return View(checklistitem);
		}

		// POST: Checklists/EditItem/5
		// To protect from overposting attacks, enable the specific properties you want to bind to.
		// For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
		[HttpPost]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> EditItem(int id, [Bind("Id,ChecklistId,Item,IsChecked,IsArchived")] ChecklistItem checklistitem)
		{
			if (id != checklistitem.Id)
			{
				return NotFound();
			}

			if (ModelState.IsValid)
			{
				try
				{
					_context.ChecklistItems.Update(checklistitem);
					await _context.SaveChangesAsync();

					//notification for edit
					if (checklistitem.Item.Length > 10)
						_notyf.Custom($"Item {checklistitem.Item.Substring(0, 10)}... is updated!", 5, "#602AC3", "fa fa-refresh");
					else
						_notyf.Custom($"Item {checklistitem.Item} is updated!", 5, "#602AC3", "fa fa-refresh");
				}
				catch (DbUpdateConcurrencyException)
				{
					if (!ChecklistExists(checklistitem.Id))
					{
						return NotFound();
					}
					else
					{
						throw;
					}
				}
			}

			return ReturnToCardByChecklist(checklistitem.ChecklistId);
		}

		// GET: Checklists/RemoveItem
		public async Task<IActionResult> RemoveItem(int? id)
        {
            if (id == null || _context.ChecklistItems == null)
            {
                return NotFound();
            }

            var checklistitem = await _context.ChecklistItems
				.Include(c => c.Checklist).ThenInclude(c => c.Card)
				.FirstOrDefaultAsync(c => c.Id == id);
			if (checklistitem == null)
            {
                return NotFound();
            }

            ViewDataReturnURL(checklistitem.Checklist.CardId);

            return View(checklistitem);
        }

        // POST: Checklists/RemoveItem
        [HttpPost, ActionName("RemoveItem")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RemoveItemConfirmed(int id)
        {
            if (_context.ChecklistItems == null)
            {
                return Problem("Entity set 'GoatrelloDataContext.ChecklistItemss'  is null.");
            }
            var checklistitem = await _context.ChecklistItems
                .Include(c => c.Checklist).ThenInclude(c => c.Card)
				.FirstOrDefaultAsync(c => c.Id == id);

			if (checklistitem != null)
            {

				var user = await GoatAuthorize.CreateFromContext(HttpContext);

				//Raise an error if the user doesn't have admin permission for this board
				if (!await user.ListingAccess(checklistitem.Checklist.Card.ListingId, GoatAuthorizeType.Write))
				{
					_notyf.Error($"You do not have permission to edit this card.", 5);
					return RedirectToAction("Details", "Cards", new { Id = checklistitem.Checklist.Card.Id });
				}


				checklistitem.IsArchived = true;
				await _context.SaveChangesAsync();

				//notification for remove
				if (checklistitem.Item.Length > 10)
					_notyf.Custom($"Item {checklistitem.Item.Substring(0, 10)}... has been removed!", 5, "#495867", "fa fa-trash");
				else
					_notyf.Custom($"Item {checklistitem.Item} has been removed!", 5, "#495867", "fa fa-trash");
			}

            return ReturnToCardByChecklist(checklistitem.ChecklistId);
        }

        // GET: Checklists/ToggleItem
        public async Task<IActionResult> ToggleItem(int? id)
        {
            if (_context.ChecklistItems == null)
            {
                return Problem("Entity set 'GoatrelloDataContext.ChecklistItemss'  is null.");
            }
            var checklistitem = await _context.ChecklistItems
                .Include(c => c.Checklist).ThenInclude(c => c.Card)
                .FirstOrDefaultAsync(c => c.Id == id);



			if (checklistitem != null)
            {
				var user = await GoatAuthorize.CreateFromContext(HttpContext);

				//Raise an error if the user doesn't have admin permission for this board
				if (!await user.ListingAccess(checklistitem.Checklist.Card.ListingId, GoatAuthorizeType.Write))
				{
					_notyf.Error($"You do not have permission to edit this card.", 5);
					return RedirectToAction("Details", "Cards", new { Id = checklistitem.Checklist.Card.Id });
				}
				checklistitem.IsChecked = !checklistitem.IsChecked;
                await _context.SaveChangesAsync();
            }
            return ReturnToCardByChecklist(checklistitem.ChecklistId);
        }

        private async void AddActionActivity(int cardId, string action)
        {
            var card = await _context.Cards
                .FirstOrDefaultAsync(m => m.Id == cardId);

            Activity activity = new Activity()
            {
                CardId = card.Id,
                AuthorId = User.GetUserId(),
                Created = DateTime.Now,
                Content = action,
                IsRecord = true,
            };
            card.Activities.Add(activity); ;
        }
        public RedirectToActionResult ReturnToCardByChecklist(int checklistId)
        {
            //Not a fan of this. Need to figure out how to tidy it up later - CS
            int cardId = _context.Checklists.Where(c => c.Id == checklistId).FirstOrDefault().CardId;

            return RedirectToAction("Details", "Cards", new { Id = cardId });
        }


        private bool ChecklistExists(int id)
        {
            return _context.Checklists.Any(e => e.Id == id);
        }
        private void ViewDataReturnURL(int CardId)
        {
            ViewData["returnURL"] = MaintainURL.ReturnURL(HttpContext, $"Cards/Details/{CardId}");
        }
    }
}
