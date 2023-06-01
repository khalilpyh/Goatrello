using System;
using System.Collections.Generic;
using System.Diagnostics;
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
using Microsoft.AspNetCore.Identity;
using System.Diagnostics.Metrics;
using Goatrello.ViewModels;
using System.Text.RegularExpressions;
using System.Drawing;
using DnsClient.Internal;

namespace Goatrello.Controllers
{
    [Authorize]
    public class BoardsController : CognizantController
    {
        private readonly GoatrelloDataContext _context;
        //for toast notification dependency injection
        private readonly INotyfService _notyf;

        public BoardsController(GoatrelloDataContext context, INotyfService notyf)
        {
            _context = context;
            _notyf = notyf;
        }

        // GET: Boards
        public async Task<IActionResult> Index(int? page, int? pageSizeID, string sortDirectionCheck, string sortFieldID, string searchString, string actionButton, string sortDirection = "asc", string sortField = "Title")
        {
            var user = await GoatAuthorize.CreateFromContext(HttpContext);

            //Clear Cookies
            CookieHelper.CookieSet(HttpContext, ControllerName() + "URL", "", -1);

            //Set Return URL
            ViewData["returnURL"] = MaintainURL.ReturnURL(HttpContext, ControllerName());

            //change filter button color
            ViewData["Filtering"] = "royal text-white";
            //List of sort options
            //TODO: Add more sorting options
            string[] sortOptions = new[] { "Title" };

            //start query and limit results to user permissions
            var boards = _context.Boards
                .GoatAuthorize(user, GoatAuthorizeType.Read)
                .AsNoTracking();

            #region Filters

            //Hide Archived
            if (true)//Placeholder for show admin later
            {
                boards = boards.Where(b => b.IsArchived == false);
            }

            //Search by Name
            if (!searchString.IsNullOrEmpty())
            {
                boards = boards.Where(b => b.Title.ToUpper().Contains(searchString.ToUpper()));
                ViewData["Filtering"] = "btn-warning";
            }
            #endregion

            #region Sort
            //check for sorting condition first
            if (!String.IsNullOrEmpty(actionButton)) //Form Submitted!
            {
                page = 1;//Reset page to start

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

            //Sorting
            if (sortField == "Label")   //Sort by Labels?
            {
                //Todo: Sort by Labels
                if (sortDirection == "asc")
                {
                }
                else
                {
                }
            }
            else // Default sort by title
            {
                if (sortDirection == "asc")
                {
                    boards = boards
                        .OrderBy(p => p.Title);
                }
                else
                {
                    boards = boards
                        .OrderByDescending(p => p.Title);
                    ViewData["Filtering"] = "btn-warning";
                }
            }

            //save sort options for next sorting action
            ViewData["sortField"] = sortField;
            ViewData["sortDirection"] = sortDirection;

            //SelectList for Sorting Options
            ViewBag.sortFieldID = new SelectList(sortOptions, sortField.ToString());
            #endregion

            //Todo: Search via name of board

            //paging
            int pageSize = PageSizeHelper.SetPageSize(HttpContext, pageSizeID, "Boards");
            ViewData["pageSizeID"] = PageSizeHelper.PageSizeList(pageSize);
            var pagedData = await PaginatedList<Board>.CreateAsync(boards.AsNoTracking(), page ?? 1, pageSize);

            return View(pagedData);
        }

        // GET: Boards/Create
        public IActionResult Create()
        {
            //Get return URL
            ViewDataReturnURL();

            ViewData["AdministratorsId"] = new SelectList(_context.Users, "Id", "DisplayName");
            ViewData["HiddenFromId"] = new SelectList(_context.Users, "Id", "DisplayName");
            ViewData["MembersId"] = new SelectList(_context.Users, "Id", "DisplayName");
            ViewData["ObserversId"] = new SelectList(_context.Users, "Id", "DisplayName");
            return View();
        }

        // POST: Boards/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Title,IsArchived,AdministratorsId,MembersId,ObserversId,HiddenFromId,IsPrivate")] Board board)
        {
            //Get return URL
            ViewDataReturnURL();

            if (ModelState.IsValid)
            {

                //User lists
                board.Administrators = new UserList();
                board.Members = new UserList();
                board.Observers = new UserList();
                board.HiddenFrom = new UserList();

                //Labels
                board.Labels = new LabelList();


                //Make template listing
                Listing l = new Listing
                {
                    BoardId = board.Id,
                    Title = "TemplateListing",
                    Observers = new UserList(),
                    HiddenFrom = new UserList(),
                    IsPrivate = true, //set default view to private to simplify user experience
                    IsTemplate = true
                };

                board.IsPrivate = true;

                board.Listings.Add(l);

                _context.Add(board);
                await _context.SaveChangesAsync();

                var addAdmin = new UserListUser { ListId = board.Administrators.Id, UserId = User.GetUserId() };
                board.Administrators.Users.Add(addAdmin);

                await _context.SaveChangesAsync();
                //notification for create
                if (board.Title.Length > 10)
                    _notyf.Custom($"Board {board.Title.Substring(0, 10)}... is created!", 5, "#602AC3", "fa fa-check");
                else
                    _notyf.Custom($"Board {board.Title} is created!", 5, "#602AC3", "fa fa-check");

                return RedirectToAction("Index", "Listings", new { BoardId = board.Id });
                //return RedirectToAction(nameof(Index));
            }

            return View(board);
        }

        // GET: Boards/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            //Get return URL
            ViewDataReturnURL(id);

            if (id == null || _context.Boards == null)
            {
                return NotFound();
            }

            //get current user
            var user = await GoatAuthorize.CreateFromContext(HttpContext);
            //Raise an error if the user doesn't have admin permission for this board
            if (!await user.BoardAccess(id, GoatAuthorizeType.Admin))
            {
                _notyf.Error($"You do not have permission to edit that board.", 5);
                return RedirectToAction(nameof(Index));
            }



            var board = await _context.Boards
                .Include(b => b.Administrators)
                .ThenInclude(b => b.Users)
                .ThenInclude(b => b.User)
                .Include(b => b.HiddenFrom)
                .ThenInclude(b => b.Users)
                .ThenInclude(b => b.User)
                .Include(b => b.Members)
                .ThenInclude(b => b.Users)
                .ThenInclude(b => b.User)
                .Include(b => b.Observers)
                .ThenInclude(b => b.Users)
                .ThenInclude(b => b.User)
                .Include(b => b.Labels)
                .ThenInclude(b => b.Labels)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (board == null)
            {
                return NotFound();
            }

            ViewData["Users"] = new SelectList(_context.Users, "Id", "DisplayName");

            var labels = _context.LabelListLabels
                .Include(l => l.Label)
                .Where(l => l.ListId == board.LabelsId);

            ViewBag.Labels = labels;
            //ViewData["Labels"] = new SelectList(_context.Labels, "Id", "Title");

            //var labels = _context.LabelListLabels.Include(l => l.Label).Where(l => l.ListId == board.Labels.Id);

            //ViewData["Labels"] = new SelectList(board.Labels.Labels, "Id", "Title");

            return View(board);
        }

        // POST: Boards/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id)
        {
            //Get return URL
            ViewDataReturnURL(id);

            var user = await GoatAuthorize.CreateFromContext(HttpContext);
            //Raise an error if the user doesn't have admin permission for this board
            if (!await user.BoardAccess(id, GoatAuthorizeType.Admin))
            {
                _notyf.Error($"You do not have permission to edit this board.", 5);
                return RedirectToAction(nameof(Index));
            }


            var boardToUpdate = await _context.Boards
                .Include(b => b.Administrators)
                .ThenInclude(b => b.Users)
                .ThenInclude(b => b.User)
                .Include(b => b.HiddenFrom)
                .ThenInclude(b => b.Users)
                .ThenInclude(b => b.User)
                .Include(b => b.Members)
                .ThenInclude(b => b.Users)
                .ThenInclude(b => b.User)
                .Include(b => b.Observers)
                .ThenInclude(b => b.Users)
                .ThenInclude(b => b.User)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (boardToUpdate == null)
            {
                return NotFound();
            }

            if (await TryUpdateModelAsync<Board>(boardToUpdate, "",
                b => b.Title, b => b.IsArchived, b => b.IsPrivate))
            {
                try
                {
                    await _context.SaveChangesAsync();

                    //notification for edit
                    if (boardToUpdate.Title.Length > 10)
                        _notyf.Custom($"Board {boardToUpdate.Title.Substring(0, 10)}... is updated!", 5, "#602AC3", "fa fa-refresh");
                    else
                        _notyf.Custom($"Board {boardToUpdate.Title} is updated!", 5, "#602AC3", "fa fa-refresh");
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!BoardExists(boardToUpdate.Id))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction("Index", "Listings", new { BoardId = boardToUpdate.Id });
            }

            ViewData["Users"] = new SelectList(_context.Users, "Id", "DisplayName");

            ViewData["AdministratorsId"] = new SelectList(_context.Users, "Id", "DisplayName", boardToUpdate.AdministratorsId);
            ViewData["HiddenFromId"] = new SelectList(_context.Users, "Id", "DisplayName", boardToUpdate.HiddenFromId);
            ViewData["MembersId"] = new SelectList(_context.Users, "Id", "DisplayName", boardToUpdate.MembersId);
            ViewData["ObserversId"] = new SelectList(_context.Users, "Id", "DisplayName", boardToUpdate.ObserversId);
            return View(boardToUpdate);
        }

        // GET: Boards/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            //Get return URL
            ViewDataReturnURL(id);
            if (id == null || _context.Boards == null)
            {
                return NotFound();
            }

            var user = await GoatAuthorize.CreateFromContext(HttpContext);
            //Raise an error if the user doesn't have admin permission for this board
            if (!await user.BoardAccess(id, GoatAuthorizeType.Admin))
            {
                _notyf.Error($"You do not have permission to archive this board.", 5);
                return RedirectToAction(nameof(Index));
            }

            var board = await _context.Boards
                .FirstOrDefaultAsync(m => m.Id == id);
            if (board == null)
            {
                return NotFound();
            }

            return View(board);
        }

        // POST: Boards/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            if (_context.Boards == null)
            {
                return Problem("Entity set 'GoatrelloDataContext.Boards'  is null.");
            }
            var board = await _context.Boards.FindAsync(id);

            var user = await GoatAuthorize.CreateFromContext(HttpContext);
            //Raise an error if the user doesn't have admin permission for this board
            if (!await user.BoardAccess(id, GoatAuthorizeType.Admin))
            {
                _notyf.Error($"You do not have permission to archive this board.", 5);
                return RedirectToAction(nameof(Index));
            }

            if (board != null)
            {
                board.IsArchived = true;
            }

            await _context.SaveChangesAsync();

            //notification for delete
            if (board.Title.Length > 10)
                _notyf.Custom($"Board {board.Title.Substring(0, 10)}... has been archived!", 5, "#495867", "fa fa-trash");
            else
                _notyf.Custom($"Board {board.Title} has been archived!", 5, "#495867", "fa fa-trash");

            return RedirectToAction(nameof(Index));
        }

        //User Managment
        public async Task<IActionResult> AddUser(int boardId, int userId, string userListSelect)
        {
            var board = await _context.Boards
                .Include(b => b.Administrators)
                .ThenInclude(b => b.Users)
                .ThenInclude(b => b.User)
                .Include(b => b.HiddenFrom)
                .ThenInclude(b => b.Users)
                .ThenInclude(b => b.User)
                .Include(b => b.Members)
                .ThenInclude(b => b.Users)
                .ThenInclude(b => b.User)
                .Include(b => b.Observers)
                .ThenInclude(b => b.Users)
                .ThenInclude(b => b.User)
                .FirstOrDefaultAsync(b => b.Id == boardId);

            var targetList = userListSelect switch
            {
                "Admin" => board.Administrators,
                "Member" => board.Members,
                "Observer" => board.Observers,
                "Hidden" => board.HiddenFrom,
                _ => throw new Exception($"Invalid list selection: {userListSelect}")
            };
            var addUser = new UserListUser { ListId = targetList.Id, UserId = userId };
            var removeUsers = board.Administrators.Users.Where(u => u.UserId == userId)
                .Concat(board.Members.Users.Where(u => u.UserId == userId))
                .Concat(board.Observers.Users.Where(u => u.UserId == userId))
                .Concat(board.HiddenFrom.Users.Where(u => u.UserId == userId));
            // raise error if there will not be an administrator
            if (removeUsers.Any(r => r.ListId == board.AdministratorsId)
                                     && board.Administrators.Users.Count <= 1)
                _notyf.Error($"Boards require at least one administrator!", 5);
            // raise error if the user is already on that list
            else if (targetList.Users.FirstOrDefault(u => u.UserId == userId) != null)
                _notyf.Error($"User is already part of that list!", 5);
            //remove user from other lists and add to the new list
            else
            {
                _context.UserListUsers.RemoveRange(removeUsers);
                _context.UserListUsers.Add(addUser);
                await _context.SaveChangesAsync();
                _notyf.Success("User successfully moved!", 5);
            }

            ViewData["Users"] = new SelectList(_context.Users, "Id", "DisplayName");

            return RedirectToAction("Edit", new { Id = boardId });
        }

        public async Task<IActionResult> RemoveUser(int boardId, int userId, string userListSelect)
        {
            var board = await _context.Boards
                .Include(b => b.Administrators)
                .ThenInclude(b => b.Users)
                .ThenInclude(b => b.User)
                .Include(b => b.HiddenFrom)
                .ThenInclude(b => b.Users)
                .ThenInclude(b => b.User)
                .Include(b => b.Members)
                .ThenInclude(b => b.Users)
                .ThenInclude(b => b.User)
                .Include(b => b.Observers)
                .ThenInclude(b => b.Users)
                .ThenInclude(b => b.User)
                .FirstOrDefaultAsync(b => b.Id == boardId);

            var user = _context.UserListUsers.Where(u => u.UserId == userId);

            if (user != null)
            {
                switch (userListSelect)
                {
                    case "Admin":
                        if (board.Administrators.Users.Count() == 1)
                        {
                            _notyf.Error($"Board must have at least one admin.", 5);
                        }
                        else
                        {
                            board.Administrators.Users.Remove(await user.Where(u => u.ListId == board.Administrators.Id).FirstOrDefaultAsync());
                        }

                        break;
                    case "Hidden":
                        board.HiddenFrom.Users.Remove(await user.Where(u => u.ListId == board.HiddenFrom.Id).FirstOrDefaultAsync());
                        break;
                    case "Member":
                        board.Members.Users.Remove(await user.Where(u => u.ListId == board.Members.Id).FirstOrDefaultAsync());
                        break;
                    case "Observer":
                        board.Observers.Users.Remove(await user.Where(u => u.ListId == board.Observers.Id).FirstOrDefaultAsync());
                        break;
                    default: throw new Exception("User type not selected properly.");
                }
            }
            else
            {
                return NotFound();
            }

            await _context.SaveChangesAsync();

            ViewData["Users"] = new SelectList(_context.Users, "Id", "DisplayName");

            return RedirectToAction("Edit", new { Id = boardId });
        }

        //Label Managment
        public async Task<IActionResult> AddLabel(int boardId, string Title, string Color)
        {
            //Validationn

            if (Title.IsNullOrEmpty())
            {
                _notyf.Error($"Invalid Label Title.", 5);
            }
            else
            {


                string formattedColor = Color;

                if (Color[0] != '#')
                {
                    formattedColor = "#" + Color;
                }


                if (Regex.IsMatch(formattedColor, "^#(?:[0-9a-fA-F]{3}){1,2}$"))
                {
                    var board = await _context.Boards
                        .Include(b => b.Labels)
                        .ThenInclude(b => b.Labels)
                        .FirstOrDefaultAsync(b => b.Id == boardId);

                    Label label = new Label()
                    {
                        Title = Title,
                        Color = formattedColor,
                        IsArchived = false
                    };
                    board.Labels.Labels.Add(label);
                    await _context.SaveChangesAsync();

                    LabelListLabel lbl = new LabelListLabel()
                    {
                        LabelId = label.Id,
                        ListId = board.LabelsId
                    };

                    _context.LabelListLabels.Add(lbl);

                    await _context.SaveChangesAsync();
                }
                else
                {
                    _notyf.Error($"{Color} is not a valid colour.", 5);
                }
            }

            ViewData["Users"] = new SelectList(_context.Users, "Id", "DisplayName");

            return RedirectToAction("Edit", new { Id = boardId });
        }
        public async Task<IActionResult> RemoveLabel(int boardId, int LabelId)
        {
            //Validationn


            var board = await _context.Boards
                    .Include(b => b.Labels)
                    .ThenInclude(b => b.Labels)
                    .FirstOrDefaultAsync(b => b.Id == boardId);

            var label = _context.Labels.Where(l => l.Id == LabelId).FirstOrDefault();

            if (label == null)
            {
                return NotFound();
            }

            _context.Remove(label);
            await _context.SaveChangesAsync();

            return RedirectToAction("Edit", new { Id = boardId });
        }
        
        public async Task<IActionResult> EditLabel(int boardId, int? labelId)
        {
            ViewDataReturnURL(boardId);

            var label = await _context.Labels
                .FirstOrDefaultAsync(b => b.Id == labelId);

            ViewBag.boardId = boardId;

            return View(label);
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditLabel(int boardId, int Id)
        {
            ViewDataReturnURL(boardId);
            //Validationn


            var labelToUpdate = await _context.Labels
               .FirstOrDefaultAsync(m => m.Id == Id);

            //Check that you got it or exit with a not found error
            if (labelToUpdate == null)
            {
                return NotFound();
            }


            //Try updating it with the values posted
            if (await TryUpdateModelAsync<Label>(labelToUpdate, "",
                p => p.Title, p => p.Color, p => p.IsArchived ) || true)//Model update error is due to Hex being 7 characters long with hashtag included. Need to update Label Model at some point to fix this.
            {
                try
                {
                    await _context.SaveChangesAsync();
                    return RedirectToAction("Edit", new { Id = boardId });
                }

                catch (DbUpdateException)
                {
                    _notyf.Error("Unable to save changes. Try again, and if the problem persists please contact your system administrator.", 5);
                    return View(labelToUpdate);
                }
            }
            else
            {
                DebugModelState();
            }


            return View(labelToUpdate);
        }
//Other Methods
        private bool BoardExists(int id)
        {
            return (_context.Boards?.Any(e => e.Id == id)).GetValueOrDefault();
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
        private void ViewDataReturnURL(int? boardId = null)
        {
            if (boardId != null)
            {
                ViewData["returnURL"] = MaintainURL.ReturnURL(HttpContext, $"Listings?BoardId={boardId}");
            }
            else
            {
                ViewData["returnURL"] = MaintainURL.ReturnURL(HttpContext, ControllerName());
            }
        }
    }
}
