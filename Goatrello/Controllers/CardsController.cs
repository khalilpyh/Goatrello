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
using System.Reflection;
using AspNetCoreHero.ToastNotification.Abstractions;
using Microsoft.AspNetCore.Routing.Template;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Hosting.Internal;
using Activity = Goatrello.Models.Activity;

namespace Goatrello.Controllers
{
    [Authorize]
    public class CardsController : CognizantController
    {
        private readonly GoatrelloDataContext _context;
        //for toast notification dependency injection
        private readonly INotyfService _notyf;
        //For file upload
        private IWebHostEnvironment _hostingEnvironment;

        public CardsController(GoatrelloDataContext context, INotyfService notyf, IWebHostEnvironment environment)
        {
            _context = context;
            _notyf = notyf;
            _hostingEnvironment = environment;
        }

        // GET: Cards
        public async Task<IActionResult> Index(int? Id,
            string SearchString, string sortDirection = "asc", string sortField = "Title")
        {
            //Clear Cookies
            CookieHelper.CookieSet(HttpContext, ControllerName() + "URL", "", -1);

            //Set Return URL
            ViewData["returnURL"] = MaintainURL.ReturnURL(HttpContext, "Listings");

            var cards = _context.Cards
                .Include(c => c.Listing)
                .Include(c => c.Checklists)
                .ThenInclude(c => c.ChecklistItems)
                .Include(c => c.Labels)
                .Include(c => c.Attachments)
                .Include(c => c.Activities)
                .Where(l => l.Id == Id.GetValueOrDefault());

            #region Filters
            //Search by Name
            if (!SearchString.IsNullOrEmpty())
            {
                cards = cards.Where(b => b.Title.ToUpper().Contains(SearchString.ToUpper()));
            }
            #endregion

            return View(await cards.ToListAsync());
        }

        // GET: Cards/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            //Get return URL
            ViewDataReturnURL();

            if (id == null || _context.Cards == null)
            {
                return NotFound();
            }

            var card = await _context.Cards
                .Include(c => c.Listing)
                .ThenInclude(c => c.Board)
                .Include(c => c.Checklists)
                .ThenInclude(c => c.ChecklistItems)
                .Include(c => c.Labels)
                .ThenInclude(c => c.Labels)
                .Include(c => c.Attachments)
                .ThenInclude(c => c.ServerFile)
                .Include(c => c.Activities)
                .ThenInclude(c => c.Author)
                .Include(c => c.Links)
                .Include(c => c.CustomFieldDatas)
                .ThenInclude(c => c.CustomField)
				.Include(b => b.Users)
				.ThenInclude(b => b.Users)
				.ThenInclude(b => b.User)
				.FirstOrDefaultAsync(m => m.Id == id);

            if (card == null)
            {
                return NotFound();
            }

            //User permission check
            var user = await GoatAuthorize.CreateFromContext(HttpContext);

            //Raise an error if the user doesn't have admin permission for this board
            if (!await user.ListingAccess(card.ListingId, GoatAuthorizeType.Write))
            {
                ViewData["WriteProtect"] = "disabled=disabled hidden=hidden";
                ViewData["WriteProtectVisible"] = "disabled=disabled ";

            }


            //List of labels on the card
            var labels = _context.LabelListLabels
                .Include(l => l.Label)
                .Where(l => l.ListId == card.LabelsId);
            if (labels != null)
            {
                ViewBag.Labels = labels;
            }

            //list of labels on the board
            var boardLabels = _context.LabelListLabels
                .Include(l => l.Label)
                .Where(l => l.ListId == card.Listing.Board.LabelsId);

            if (boardLabels != null)
            {
                ViewBag.BoardLabels = boardLabels;
            }

            //Other listings that the card can be moved to
            var boardListings = _context.Listings
                .Where(l => l.BoardId == card.Listing.BoardId && l.IsTemplate == false && l.Id != card.Listing.Id);

            if (boardListings != null)
            {
                ViewBag.BoardListings = boardListings;
            }

            //Custom Fields
            var customFields = from c in _context.CustomFields select c;

            if (customFields != null)
            {
                ViewBag.CustomFields = customFields;
            }

            //Users
            var users = _context.Users;

            if (users != null)
            {
                ViewBag.Users = _context.Users;
            }
            

            //card due date notification
            if (card.DueDate.HasValue)
            {
                if (!card.IsDueDateComplete)
                {
                    if (card.DueDate < DateTime.Now)
                        _notyf.Custom("This card has passed the due date!", 5, "#c32f27", "fa fa-exclamation-triangle");
                    else if (card.DueDate >= DateTime.Now && card.DueDate < DateTime.Now.AddDays(7))
                        _notyf.Custom("This card will be due soon!", 5, "#602AC3", "fa fa-clock-o");
                    else
                        _notyf.Custom("Due date is coming!", 5, "#602AC3", "fa fa fa-calendar");
                }
                else
                    _notyf.Custom("This card is completed!", 5, "#0c343d", "fa fa-smile-o");
            }


            return View(card);
        }

        // GET: Cards/Create
        public IActionResult Create(int ListingId)
        {
            ViewDataReturnURL();

            Card c = new Card()
            {
                ListingId = ListingId
            };
            return View(c);
        }

        // POST: Cards/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Id,ListingId,Title,Description,DueDate,Reminder,IsDueDateComplete,IsArchived")] Card card)
        {
            //Get return URL
            ViewDataReturnURL();

            var user = await GoatAuthorize.CreateFromContext(HttpContext);

            //Raise an error if the user doesn't have admin permission for this board
            if (!await user.ListingAccess(card.ListingId, GoatAuthorizeType.Write))
            {
                _notyf.Error($"You do not have permission to create a card on this Listing.", 5);
                return RedirectToAction("Index", "Listings", new { Id = card.ListingId });
            }

            if (ModelState.IsValid)
            {
                AddActionActivity(card, "created this card.");
                card.Labels = new LabelList();
                _context.Add(card);
                await _context.SaveChangesAsync();

                //notification for create
                if (card.Title.Length > 10)
                    _notyf.Custom($"Card {card.Title.Substring(0, 10)}... is created!", 5, "#602AC3", "fa fa-check");
                else
                    _notyf.Custom($"Card {card.Title} is created!", 5, "#602AC3", "fa fa-check");

                //return RedirectToAction(nameof(Index));
                return RedirectToAction("Details", new { card.Id });
            }
            return View(card);
        }

        // GET: Cards/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            //Get return URL
            ViewDataReturnURL();

            if (id == null || _context.Cards == null)
            {
                return NotFound();
            }

            var card = await _context.Cards
                .Include(c => c.Listing)
                .Include(c => c.Checklists)
                .ThenInclude(c => c.ChecklistItems)
                .Include(c => c.Labels)
                .Include(c => c.Attachments)
                .Include(c => c.Activities)
                .ThenInclude(c => c.Author)
                .FirstOrDefaultAsync(c => c.Id == id);

            if (card == null)
            {
                return NotFound();
            }
            var user = await GoatAuthorize.CreateFromContext(HttpContext);

            //Raise an error if the user doesn't have admin permission for this board
            if (!await user.ListingAccess(card.ListingId, GoatAuthorizeType.Write))
            {
                _notyf.Error($"You do not have permission to edit this card.", 5);
                return RedirectToAction("Details", new { card.Id });
            }


            ViewData["ListingId"] = new SelectList(_context.Listings.Where(l => l.BoardId == card.Listing.BoardId && l.IsTemplate == card.Listing.IsTemplate), "Id", "Title", card.ListingId);

            return View(card);
        }

        // POST: Cards/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, List<IFormFile> theFiles, string? editType)
        {
            //Get return URL
            ViewDataReturnURL();

            //Get Card to update
            var cardToUpdate = await _context.Cards
                .Include(c => c.Listing)
                .Include(c => c.Checklists)
                .ThenInclude(c => c.ChecklistItems)
                .Include(c => c.Labels)
                .Include(c => c.Attachments)
                .ThenInclude(a => a.ServerFile)
                .Include(c => c.Activities)
                .ThenInclude(c => c.Author)
                .FirstOrDefaultAsync(c => c.Id == id);

            if (cardToUpdate == null)
            {
                return NotFound();
            }

            var user = await GoatAuthorize.CreateFromContext(HttpContext);

            //Raise an error if the user doesn't have admin permission for this board
            if (!await user.ListingAccess(cardToUpdate.ListingId, GoatAuthorizeType.Write))
            {
                _notyf.Error($"You do not have permission to edit this card.", 5);
                return RedirectToAction("Details", new { cardToUpdate.Id });
            }


            string oldListing = "";
            //store listing name if moving card
            if (editType == "listing")
            {
                oldListing = cardToUpdate.Listing.Title;
            }

            if (await TryUpdateModelAsync<Card>(cardToUpdate, "",
                c => c.ListingId, c => c.Title, c => c.Description, c => c.DueDate,
                c => c.Reminder, c => c.IsDueDateComplete, c => c.Attachments, c => c.IsArchived))
            {
                try
                {
                    string editAction = "";
                    switch (editType)
                    {
                        case "title":
                            editAction = "edited the title.";
                            break;

                        case "description":
                            editAction = "edited the description.";
                            break;
                        case "dueDate":
                            editAction = "edited the due date.";
                            break;
                        case "reminder":
                            editAction = "set a reminder on this card.";
                            break;
                        case "attachment":
                            editAction = "added an attachment.";
                            break;
                        case "listing":
                            editAction = $"moved this card from {oldListing} to {cardToUpdate.Listing.Title}.";
                            break;
                        default:
                            editAction = "edited this card.";
                            break;
                    }

                    //Do not add "edited" activity if card is template.
                    if (cardToUpdate.Listing.IsTemplate != true)
                    {
                        AddActionActivity(cardToUpdate, editAction);
                    }

                    //_context.Update(card);
                    await AddDocumentsAsync(cardToUpdate, theFiles);
                    await _context.SaveChangesAsync();

                    //notification for edit
                    if (cardToUpdate.Title.Length > 10)
                        _notyf.Custom($"Card {cardToUpdate.Title.Substring(0, 10)}... is updated!", 5, "#602AC3", "fa fa-refresh");
                    else
                        _notyf.Custom($"Card {cardToUpdate.Title} is updated!", 5, "#602AC3", "fa fa-refresh");

                    return RedirectToAction("Details", new { cardToUpdate.Id });
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (cardToUpdate == null)
                    {
                        return NotFound();
                    }
                    else
                    {

                        throw;
                    }
                }
            }
            else
            {
                DebugModelState();
            }

            ViewData["ListingId"] = new SelectList(_context.Users, "Id", "Title", cardToUpdate.ListingId);

            return View(cardToUpdate);
        }

        // GET: Cards/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            //Get return URL
            ViewDataReturnURL();

            if (id == null || _context.Cards == null)
            {
                return NotFound();
            }

            var card = await _context.Cards
                .Include(c => c.Listing)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (card == null)
            {
                return NotFound();
            }

            var user = await GoatAuthorize.CreateFromContext(HttpContext);

            //Raise an error if the user doesn't have admin permission for this board
            if (!await user.ListingAccess(card.ListingId, GoatAuthorizeType.Write))
            {
                _notyf.Error($"You do not have permission to edit this card.", 5);
                return RedirectToAction("Details", new { card.Id });
            }


            return View(card);
        }


        // POST: Cards/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            //Get return URL
            ViewDataReturnURL();

            if (_context.Cards == null)
            {
                return Problem("Entity set 'GoatrelloDataContext.Cards'  is null.");
            }
            var card = await _context.Cards
                .Include(c => c.Listing)
                .FirstOrDefaultAsync(m => m.Id == id);


            if (card != null)
            {
                var user = await GoatAuthorize.CreateFromContext(HttpContext);

                //Raise an error if the user doesn't have admin permission for this board
                if (!await user.ListingAccess(card.ListingId, GoatAuthorizeType.Write))
                {
                    _notyf.Error($"You do not have permission to edit this card.", 5);
                    return RedirectToAction("Details", new { card.Id });
                }

                AddActionActivity(card, "archived this card.");
                card.IsArchived = true;
            }

            await _context.SaveChangesAsync();

            //notification for delete
            if (card.Title.Length > 10)
                _notyf.Custom($"Card {card.Title.Substring(0, 10)}... has been archived from {card.Listing.Title}!", 5, "#495867", "fa fa-trash");
            else
                _notyf.Custom($"Card {card.Title} has been archived from {card.Listing.Title}!", 5, "#495867", "fa fa-trash");

            //return RedirectToAction(nameof(Index));
            return RedirectToAction("Index", "Listings", new { BoardId = card.Listing.BoardId });
        }
        public async Task<IActionResult> MakeTemplate(int CardId)
        {


            ViewDataReturnURL();

            var card = await _context.Cards
                .Include(c => c.Listing)
                .ThenInclude(l => l.Board)
                .ThenInclude(b => b.Listings)
                .Include(c => c.Checklists)
                .ThenInclude(c => c.ChecklistItems)
                .Include(c => c.Labels)
                .Include(c => c.Attachments)
                .ThenInclude(c => c.ServerFile)
                .Include(c => c.Activities)
                .ThenInclude(c => c.Author)
                .Include(c => c.Links)
                .FirstOrDefaultAsync(m => m.Id == CardId);

            var templateListing = card.Listing.Board.Listings.Where(l => l.IsTemplate == true).FirstOrDefault();

            if (card == null || templateListing == null)
            {
                return NotFound();
            }

            var user = await GoatAuthorize.CreateFromContext(HttpContext);

            //Raise an error if the user doesn't have admin permission for this board
            if (!await user.ListingAccess(card.ListingId, GoatAuthorizeType.Write))
            {
                _notyf.Error($"You do not have permission to edit this card.", 5);
                return RedirectToAction("Details", new { card.Id });
            }

            Card c = new Card()
            {
                ListingId = templateListing.Id,
                Title = card.Title,
                Description = card.Description,
                Users = card.Users,
                Labels = card.Labels,
                Checklists = new List<Checklist>(),
                DueDate = card.DueDate,
                Reminder = card.Reminder,
                IsDueDateComplete = card.IsDueDateComplete,
                Attachments = card.Attachments,
                Links = card.Links,
                IsArchived = false
            };

            if (ModelState.IsValid)
            {
                templateListing.Cards.Add(c);
                await _context.SaveChangesAsync();
                await CloneChecklists(card, c);

                //notification for making template
                if (c.Title.Length > 10)
                    _notyf.Custom($"Template Card {c.Title.Substring(0, 10)}... is created!", 5, "#602AC3", "fa fa-check");
                else
                    _notyf.Custom($"Template Card {c.Title} is created!", 5, "#602AC3", "fa fa-check");
            }


            return RedirectToAction("Details", new { c.Id });
        }

        // GET: Cards/CreateFromTemplate
        public async Task<IActionResult> CreateFromTemplate(int ListingId, int TemplateId)
        {
            ViewDataReturnURL();

            var card = await _context.Cards
                .Include(c => c.Listing)
                .Include(c => c.Checklists)
                .ThenInclude(c => c.ChecklistItems)
                .Include(c => c.Labels)
                .Include(c => c.Attachments)
                .ThenInclude(c => c.ServerFile)
                .Include(c => c.Activities)
                .ThenInclude(c => c.Author)
                .Include(c => c.Links)
                .FirstOrDefaultAsync(m => m.Id == TemplateId);

            var listing = _context.Listings.Where(l => l.Id == ListingId).FirstOrDefault();

            if (card == null || listing == null)
            {
                return NotFound();
            }

            var user = await GoatAuthorize.CreateFromContext(HttpContext);

            //Raise an error if the user doesn't have write permission for this board
            if (!await user.ListingAccess(ListingId, GoatAuthorizeType.Write))
            {
                _notyf.Error($"You do not have permission to create a card on this Listing.", 5);
                return RedirectToAction("Details", new { card.Id });
            }

            Card c = new Card()
            {
                ListingId = ListingId,
                Title = card.Title,
                Description = card.Description,
                Users = card.Users,
                Checklists = new List<Checklist>(),
                Labels = card.Labels,
                DueDate = card.DueDate,
                Reminder = card.Reminder,
                IsDueDateComplete = card.IsDueDateComplete,
                Attachments = card.Attachments,
                Links = card.Links,
                IsArchived = false
            };
            AddActionActivity(c, "created this card from a template.");
            listing.Cards.Add(c);
            await _context.SaveChangesAsync();
            await CloneChecklists(card, c);

            return RedirectToAction("Details", new { c.Id });
        }
        //Add Document Function

        public async Task AddDocumentsAsync(Card card, List<IFormFile> theFiles)
        {

            //this is basically copy-pasted and doesn't work currently. Will fix down the road.
            string uploads = Path.Combine(_hostingEnvironment.WebRootPath, "uploads");
            foreach (IFormFile file in theFiles)
            {
                if (file != null)
                {
                    //string uniqueFileName = Guid.NewGuid().ToString() + "_" + file.FileName;
                    //just use the guid as this is what is stored in the db
                    var uniqueFileName = Guid.NewGuid();
                    string filePath = Path.Combine(uploads, uniqueFileName.ToString());

                    var stream = new FileStream(filePath, FileMode.Create);
					file.CopyTo(stream);
                    Attachment attachment = new Attachment()
                    {
                        CardId = card.Id,
                        ServerFile = new ServerFile()
                        {
                            Title = file.FileName,
                            FileType = file.ContentType,
                            //store the guid directly in this field
                            FileGuid = uniqueFileName,
                            IsArchived = false,
                            UserId = User.GetUserId()
                        }
                    };
                    _context.Attachments.Add(attachment);

                    await _context.SaveChangesAsync();
                    stream.Close();
                }
            }
        }
        public async Task<FileStreamResult> Download(int id)
        {
            //Validation goes here.

            //get file
            var theFile = await _context.ServerFiles
                .Where(f => f.Id == id)
                .FirstOrDefaultAsync();

            string uploads = Path.Combine(_hostingEnvironment.WebRootPath, "uploads");

            string filePath = Path.Combine(uploads, theFile.FileGuid.ToString());

            Stream stream = new FileStream(filePath, FileMode.Open, FileAccess.Read);
            var fileDownload = new FileStreamResult(stream, theFile.FileType);

            fileDownload.FileDownloadName = theFile.Title;
            return fileDownload;
        }

        public async Task<IActionResult> ArchiveFile(int id)
        {
            //Validation goes here.

            //get file
            var attachment = await _context.Attachments
                .Include(a => a.ServerFile)
                .FirstOrDefaultAsync(f => f.Id == id);

            if (attachment == null)
            {
                return NotFound();
            }

            //Set to !IsArchived so same method can be used for un-archiving
            attachment.IsArchived = !attachment.IsArchived;
            attachment.ServerFile.IsArchived = !attachment.ServerFile.IsArchived;

            await _context.SaveChangesAsync();

            return RedirectToAction("Details", new { Id = attachment.CardId }); ;
        }
        //Activity/Comment methods
        private void AddActionActivity(Card card, string action)
        {
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

        public async Task<IActionResult> AddComment(int CardId, string comment)
        {

            if (!CardExists(CardId))
            {
                return NotFound();
            }

            var card = await _context.Cards
                .Include(c => c.Activities)
                .FirstOrDefaultAsync(m => m.Id == CardId);

            var currentUser = await GoatAuthorize.CreateFromContext(HttpContext);

            //Raise an error if the user doesn't have write permission for this board
            if (!await currentUser.ListingAccess(card.ListingId, GoatAuthorizeType.Write))
            {
                _notyf.Error($"You do not have permission to comment on this card.", 5);
                return RedirectToAction("Details", new { card.Id });
            }

            Activity activity = new Activity()
            {
                CardId = card.Id,
                AuthorId = User.GetUserId(),
                Created = DateTime.Now,
                Content = $"commented: {comment}",
                IsRecord = false
            };

            card.Activities.Add(activity);

            await _context.SaveChangesAsync();

            return RedirectToAction("Details", new { id = CardId });
        }
        public async Task<IActionResult> EditComment(int CardId, int commentId, string comment)
        {
            var commentToUpdate = await _context.Activities
                .FirstOrDefaultAsync(a => a.Id == commentId);

            var card = await _context.Cards
                .Include(c => c.Activities)
                .FirstOrDefaultAsync(m => m.Id == CardId);

            if (card == null || commentToUpdate == null)
            {
                return NotFound();
            }

            var currentUser = await GoatAuthorize.CreateFromContext(HttpContext);

            //Raise an error if the user doesn't have write permission for this board
            if (!await currentUser.ListingAccess(card.ListingId, GoatAuthorizeType.Write))
            {
                _notyf.Error($"You do not have permission to comment on this card.", 5);
                return RedirectToAction("Details", new { card.Id });
            }

            if (!comment.IsNullOrEmpty())
            {
                commentToUpdate.Content = $"commented: {comment}";
				commentToUpdate.IsEdited = true;

                _context.Activities.Update(commentToUpdate);
                await _context.SaveChangesAsync();
				_notyf.Success($"Comment edited", 5);
			}
            else
            {
				_notyf.Success($"Invalid comment edit, please try again.", 5);
			}



            return RedirectToAction("Details", new { id = CardId });
        }
        //Current relationship does not allow for deleting comments. Would need to be cascade delete
        //public async Task<IActionResult> RemoveComment(int CardId, int commentId)
        //{
            
        //    var card = await _context.Cards
        //        .Include(c => c.Activities)
        //        .FirstOrDefaultAsync(m => m.Id == CardId);

        //    var comment = await _context.Activities
        //        .FirstOrDefaultAsync(c => c.Id == commentId); 

        //    if(card == null)
        //    {
        //        return NotFound();
        //    }

        //    var currentUser = await GoatAuthorize.CreateFromContext(HttpContext);

        //    //Raise an error if the user doesn't have admin permission for this board
        //    if (!await currentUser.ListingAccess(card.ListingId, GoatAuthorizeType.Write))
        //    {
        //        _notyf.Error($"You do not have permission edit this card.", 5);
        //        return RedirectToAction("Details", new { card.Id });
        //    }

        //    if (currentUser.Id == comment.AuthorId || currentUser.IsSiteAdmin)
        //    {
        //        card.Activities.Remove(comment);

        //        await _context.SaveChangesAsync();
        //    }
        //    else
        //    {
        //        _notyf.Error($"Comments can only be removed by the author or a site admin.", 5);
        //    }



        //    return RedirectToAction("Details", new { id = CardId });
        //}

        //Label Managment
        public async Task<IActionResult> AddLabel(int cardId, int labelId)
        {

            var card = await _context.Cards
                .Include(b => b.Labels)
                .ThenInclude(b => b.Labels)
                .FirstOrDefaultAsync(b => b.Id == cardId);

            var label = _context.Labels
                .FirstOrDefault(l => l.Id == labelId);


            LabelListLabel lbl = new LabelListLabel()
            {
                LabelId = label.Id,
                ListId = card.LabelsId
            };

            _context.LabelListLabels.Add(lbl);

            await _context.SaveChangesAsync();




            return RedirectToAction("Details", new { Id = cardId });
        }
        public async Task<IActionResult> RemoveLabel(int cardId, int labelId)
        {
            //Validationn
            var card = await _context.Cards
                    .Include(b => b.Labels)
                    .ThenInclude(b => b.Labels)
                    .FirstOrDefaultAsync(b => b.Id == cardId);

            var label = _context.Labels
                .FirstOrDefault(l => l.Id == labelId);


            if (card == null || label == null)
            {
                return NotFound();
            }

            var labelListLabel = await _context.LabelListLabels
                    .FirstOrDefaultAsync(l => l.ListId == card.LabelsId && l.LabelId == label.Id);

            if (labelListLabel == null)
            {
                return NotFound();
            }

            _context.Remove(labelListLabel);
            await _context.SaveChangesAsync();

            return RedirectToAction("Details", new { Id = cardId });
        }

        //Link Management
        public async Task<IActionResult> AddLink(int cardId, string linkURI)
        {

            var card = await _context.Cards
                .Include(b => b.Links)
                .FirstOrDefaultAsync(b => b.Id == cardId);

            if (card == null)
            {
                return NotFound();
            }

            Uri uriResult;
            if (Uri.TryCreate(linkURI, UriKind.Absolute, out uriResult))
            {
                Link link = new Link()
                {
                    CardId = cardId,
                    Uri = uriResult,
                    IsArchived = false
                };
                AddActionActivity(card, "added a link.");
                _context.Links.Add(link);
                await _context.SaveChangesAsync();
            }
            else
            {
                _notyf.Error("Unable to add link. Please try again.", 5);
            }

            return RedirectToAction("Details", new { Id = cardId });
        }
        public async Task<IActionResult> EditLink(int cardId, int linkId, string linkURI)
        {

            var card = await _context.Cards
                .Include(b => b.Links)
                .FirstOrDefaultAsync(b => b.Id == cardId);

            var linkToUpdate = await _context.Links
                .FirstOrDefaultAsync(b => b.Id == linkId);

            if (card == null || linkToUpdate == null)
            {
                return NotFound();
            }

            Uri uriResult;
            if (Uri.TryCreate(linkURI, UriKind.Absolute, out uriResult))
            {
                linkToUpdate.Uri = uriResult;
                AddActionActivity(card, "edited a link.");
                await _context.SaveChangesAsync();
            }
            else
            {
                _notyf.Error("Unable to edit link. Please try again.", 5);
            }

            return RedirectToAction("Details", new { Id = linkToUpdate.CardId });
        }
        public async Task<IActionResult> ArchiveLink(int cardId, int linkId)
        {
            var card = await _context.Cards
                .Include(b => b.Links)
                .FirstOrDefaultAsync(b => b.Id == cardId);

            var link = await _context.Links
                .FirstOrDefaultAsync(b => b.Id == linkId);

            if (card == null || link == null)
            {
                return NotFound();
            }

            //Set to !IsArchived so same method can be used for un-archiving
            link.IsArchived = !link.IsArchived;
            AddActionActivity(card, "archived a link.");
            await _context.SaveChangesAsync();

            return RedirectToAction("Details", new { Id = link.CardId });
        }

        //Link Management
        public async Task<IActionResult> AddCustomFieldData(int cardId, int customFieldId, string value)
        {

            var card = _context.Cards.FirstOrDefault(b => b.Id == cardId);

            var customField = _context.CustomFields.FirstOrDefault(b => b.Id == customFieldId);

            if (card == null || customField == null)
            {
                return NotFound();
            }

            try
            {
                CustomFieldData field = new CustomFieldData()
                {
                    CardId = cardId,
                    CustomFieldId = customFieldId,
                    Value = value
                };

                _context.CustomFieldDatas.Add(field);


                AddActionActivity(card, $"added '{customField.Name}' to this card.");



                await _context.SaveChangesAsync();

            }
            catch
            {
                _notyf.Error("Unable to add custom data. Please try again later.", 5);
            }

            return RedirectToAction("Details", new { Id = cardId });
        }
        public async Task<IActionResult> EditCustomFieldData(int cardId, int customFieldId)
        {

            var card = _context.Cards.FirstOrDefault(b => b.Id == cardId);

            var customField = await _context.CustomFieldDatas
                .Include(c => c.CustomField)
                .FirstOrDefaultAsync(b => b.CardId == cardId && b.CustomFieldId == customFieldId);

            if (card == null || customField == null)
            {
                return NotFound();
            }
            try
            {
                if (await TryUpdateModelAsync<CustomFieldData>(customField, "",
                            b => b.Value))
                {

                    AddActionActivity(card, $"edited the value for '{customField.CustomField.Name}'.");

                    await _context.SaveChangesAsync();

                    //notification for edit
                    if (customField.CustomField.Name.Length > 10)
                        _notyf.Custom($"{customField.CustomField.Name.Substring(0, 10)}... has been updated.", 5, "#602AC3", "fa fa-refresh");
                    else
                        _notyf.Custom($"{customField.CustomField.Name} has been updated.", 5, "#602AC3", "fa fa-refresh");
                }
            }
            catch
            {
                _notyf.Error("Unable to add link. Please try again.", 5);
            }

            return RedirectToAction("Details", new { Id = cardId });
        }
        public async Task<IActionResult> DeleteCustomFieldData(int cardId, int customFieldId)
        {

            var card = _context.Cards.FirstOrDefault(b => b.Id == cardId);

            var customField = await _context.CustomFieldDatas
                .Include(c => c.CustomField)
                .FirstOrDefaultAsync(b => b.CardId == cardId && b.CustomFieldId == customFieldId);

            if (card == null || customField == null)
            {
                return NotFound();
            }
            try
            {

                AddActionActivity(card, $"removed '{customField.CustomField.Name}' from this card.");

                _context.CustomFieldDatas.Remove(customField);
                await _context.SaveChangesAsync();

                //notification for edit
                if (customField.CustomField.Name.Length > 10)
                    _notyf.Custom($"{customField.CustomField.Name.Substring(0, 10)}... has been removed.", 5, "#602AC3", "fa fa-refresh");
                else
                    _notyf.Custom($"{customField.CustomField.Name} has been removed.", 5, "#602AC3", "fa fa-refresh");

            }
            catch
            {
                _notyf.Error("Unable to add link. Please try again.", 5);
            }

            return RedirectToAction("Details", new { Id = cardId });
        }

        //User Management

        public async Task<IActionResult> AddUser(int cardId, int userId)
        {
            var card = await _context.Cards
                .Include(b => b.Users)
                .ThenInclude(b => b.Users)
                .ThenInclude(b => b.User)
                .FirstOrDefaultAsync(b => b.Id == cardId);

			var currentuser = await GoatAuthorize.CreateFromContext(HttpContext);

			//Raise an error if the user doesn't have admin permission for this board
			if (!await currentuser.ListingAccess(card.ListingId, GoatAuthorizeType.Write))
			{
				_notyf.Error($"You do not have permission to edit this card.", 5);
				return RedirectToAction("Details", new { card.Id });
			}

			var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Id == userId);

            if(card == null | user == null)
            {
                return NotFound();
            }

            if(card.Users == null)
            {
                card.Users = new UserList();
                _context.Cards.Update(card);
				await _context.SaveChangesAsync();
			}

            var addUser = new UserListUser { ListId = card.Users.Id, UserId = userId };

            // raise error if the user is already on that list
            if (card.Users.Users.FirstOrDefault(u => u.UserId == userId) != null)
            {
                _notyf.Error($"User is already tagged on this card!", 5);
            }
            else
            {
                _context.UserListUsers.Add(addUser);
                AddActionActivity(card, $"tagged {user.DisplayName} on this card.");

				await _context.SaveChangesAsync();
                _notyf.Success("User successfully added!", 5);
            }

            ViewBag.Users = _context.Users;

			return RedirectToAction("Details", new { Id = cardId });
		}

        public async Task<IActionResult> RemoveUser(int cardId, int userId)
        {
            var card = await _context.Cards
                .Include(b => b.Users)
                .ThenInclude(b => b.Users)
                .ThenInclude(b => b.User)
                .FirstOrDefaultAsync(b => b.Id == cardId);
            
			var currentuser = await GoatAuthorize.CreateFromContext(HttpContext);

			//Raise an error if the user doesn't have admin permission for this board
			if (!await currentuser.ListingAccess(card.ListingId, GoatAuthorizeType.Write))
			{
				_notyf.Error($"You do not have permission to edit this card.", 5);
				return RedirectToAction("Details", new { card.Id });
			}

            var user = await _context.UserListUsers
                .Include(u => u.User)
                .FirstOrDefaultAsync(u => u.UserId == userId && u.ListId == card.Users.Id);

            if (card == null || user == null)
            {
                return NotFound();
            }

			AddActionActivity(card, $"untagged {user.User.DisplayName}");

			card.Users.Users.Remove(user);

            await _context.SaveChangesAsync();

            ViewBag.Users = _context.Users;

			return RedirectToAction("Details", new { Id = cardId });
		}
        //Other Methods
        private async Task CloneChecklists(Card cardOld, Card cardNew)
        {
            cardNew.Checklists = new List<Checklist>();

            foreach (var checklist in cardOld.Checklists)
            {
                Checklist chk = new Checklist()
                {
                    CardId = cardNew.Id,
                    Title = checklist.Title,
                    ChecklistItems = new List<ChecklistItem>(),
                    IsShowHidden = checklist.IsShowHidden,
                    IsArchived = checklist.IsArchived
                };

                _context.Checklists.Add(chk);
                await _context.SaveChangesAsync();
                foreach (ChecklistItem item in checklist.ChecklistItems)
                {
                    ChecklistItem newItem = new ChecklistItem()
                    {
                        ChecklistId = chk.Id,
                        Item = item.Item,
                        IsChecked = false,
                        IsArchived = false
                    };
                    _context.ChecklistItems.Add(newItem);
                }
            }
            await _context.SaveChangesAsync();
        }
        private void DebugModelState()
        {
            //Collect ModelState errors and throw as exception
            var errors = ModelState.Values.SelectMany(v => v.Errors);

            string output = string.Empty;
            foreach (var e in errors)
            {
                output += e.ErrorMessage;
            }
            throw new Exception(output);
        }

        private bool CardExists(int id)
        {
            return (_context.Cards?.Any(e => e.Id == id)).GetValueOrDefault();
        }

        private void ViewDataReturnURL()
        {
            ViewData["returnURL"] = MaintainURL.ReturnURL(HttpContext, "Listings");
        }
    }
}
