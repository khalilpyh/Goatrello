using AspNetCoreHero.ToastNotification.Abstractions;
using Goatrello.Data;
using Goatrello.Models;
using Goatrello.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Composition;
using System.Diagnostics;
using System.Drawing.Printing;
using Microsoft.AspNetCore.Mvc.Rendering;
using MongoDB.Driver;
using System.Reflection;
using static System.Runtime.InteropServices.JavaScript.JSType;
using Amazon.Runtime.Internal.Transform;
using AspNetCoreHero.ToastNotification.Helpers;
using Microsoft.EntityFrameworkCore.Storage;

namespace Goatrello.Controllers
{
	public class ReportsController : CognizantController
	{
		private readonly GoatrelloDataContext _context;
		//for toast notification dependency injection
		private readonly INotyfService _notyf;

		public ReportsController(GoatrelloDataContext context, INotyfService notyf)
		{
			_context = context;
			_notyf = notyf;
		}

		#region Reports
		// GET: ReportsController
		public async Task<IActionResult> Index()
		{
			//Check if user is Admin.
			if (!UserIsAdmin(out IActionResult isAdmin)) return isAdmin;

			return View(await _context.Reports.ToListAsync());
		}

		// GET: ReportsController/Details/5
		public async Task<IActionResult> DetailsReport(int? Id)
		{
			//Check if user is Admin.
			if (!UserIsAdmin(out IActionResult isAdmin)) return isAdmin;

			//Get return URL
			ViewDataReturnURL();

			if (Id == null || _context.Reports == null)
			{
				return NotFound();
			}

			var report = await _context.Reports
				.Include(r => r.Filters)
				.Include(r => r.Operations)
				.Include(r => r.Results)
				.FirstOrDefaultAsync(r => r.Id == Id);

			if (report == null)
			{
				return NotFound();
			}

			return View(report);
		}

		// GET: ReportsController/Create
		public IActionResult CreateReport()
		{
			//Check if user is Admin.
			if (!UserIsAdmin(out IActionResult isAdmin)) return isAdmin;

			//Get return URL
			ViewDataReturnURL();

			return View();
		}

		// POST: ReportsController/Create
		[HttpPost]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> CreateReport([Bind("Name, IsArchived")] Report report)
		{

			//Check if user is Admin.
			if (!UserIsAdmin(out IActionResult isAdmin)) return isAdmin;

			//Get return URL
			ViewDataReturnURL();
			try
			{
				if (ModelState.IsValid)
				{

					_context.Reports.Add(report);
					await _context.SaveChangesAsync();

					//notification for create
					if (report.Name.Length > 10)
						_notyf.Custom($"Report {report.Name.Substring(0, 10)}... was created!", 5, "#602AC3", "fa fa-check");
					else
						_notyf.Custom($"Report {report.Name} was created!", 5, "#602AC3", "fa fa-check");

					PopulateDropdowns();
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

			return View("EditReport", report);
		}

		// GET: ReportsController/Edit/5
		public async Task<IActionResult> EditReport(int? Id)
		{
			//Check if user is Admin.
			if (!UserIsAdmin(out IActionResult isAdmin)) return isAdmin;

			//Get return URL
			ViewDataReturnURL();

			var report = await _context.Reports
				.Include(r => r.Filters)
				.Include(r => r.Operations)
				.FirstOrDefaultAsync(r => r.Id == Id);

			if (report == null)
			{
				return NotFound();
			}

			PopulateDropdowns();

			return View(report);
		}

		// POST: ReportsController/Edit/5
		[HttpPost]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> EditReport(int Id)
		{
			//Check if user is Admin.
			if (!UserIsAdmin(out IActionResult isAdmin)) return isAdmin;

			var reportToUpdate = await _context.Reports
				.Include(r => r.Filters)
				.Include(r => r.Operations)
				.FirstOrDefaultAsync(r => r.Id == Id);

			if (await TryUpdateModelAsync<Report>(reportToUpdate, "",
				r => r.Name, r => r.IsArchived))
			{
				try
				{
					await _context.SaveChangesAsync();

					if (reportToUpdate.Name.Length > 10)
						_notyf.Custom($"Report {reportToUpdate.Name.Substring(0, 10)}... is updated!", 5, "#602AC3", "fa fa-refresh");
					else
						_notyf.Custom($"Report {reportToUpdate.Name} is updated!", 5, "#602AC3", "fa fa-refresh");
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

			return RedirectToAction(nameof(Index));

		}

		// GET: ReportsController/Delete/5
		public async Task<IActionResult> DeleteReport(int? Id)
		{
			//Check if user is Admin.
			if (!UserIsAdmin(out IActionResult isAdmin)) return isAdmin;

			//Get return URL
			ViewDataReturnURL();

			if (Id == null || _context.Reports == null)
			{
				return NotFound();
			}


			var report = await _context.Reports
				.FirstOrDefaultAsync(m => m.Id == Id);

			if (report == null)
			{
				return NotFound();
			}

			return View(report);
		}

		// POST: ReportsController/Delete/5
		[HttpPost, ActionName("DeleteReport")]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> DeleteReportConfirmed(int Id)
		{
			//Check if user is Admin.
			if (!UserIsAdmin(out IActionResult isAdmin)) return isAdmin;

			if (_context.Reports == null)
			{
				return Problem("Entity set 'GoatrelloDataContext.Reports'  is null.");
			}

			var report = await _context.Reports.FindAsync(Id);

			if (report == null)
			{
				return NotFound();
			}

			try
			{

				report.IsArchived = !report.IsArchived;

				await _context.SaveChangesAsync();

				//notification for delete
				if (report.Name.Length > 10)
					_notyf.Custom($"Report {report.Name.Substring(0, 10)}... has been archived!", 5, "#495867", "fa fa-trash");
				else
					_notyf.Custom($"Report {report.Name} has been archived!", 5, "#495867", "fa fa-trash");

			}
			catch
			{
				//notification for delete
				_notyf.Custom($"Unable to archive report. Please try again later or contain and admin if the issue persists.", 5, "#495867", "fa fa-trash");
			}


			return RedirectToAction(nameof(Index));
		}
		#endregion Reports


		//public async Task<IActionResult> AddFilter(int reportId, string filterValue, string DataBool, int? DataId)
		public async Task<IActionResult> AddFilter(int reportId, string filterValue, List<string> dataIn)
		{
			Debug.WriteLine("here");
			//Check if user is Admin.
			if (!UserIsAdmin(out IActionResult isAdmin)) return isAdmin;

			var report = await _context.Reports
				.Include(r => r.Filters)
				.FirstOrDefaultAsync(r => r.Id == reportId);
			if (report == null)
				_notyf.Error($"Unable to add this filter.", 5);
			else
			{
				ReportFilter newFilter = new ReportFilter()
				{
					Name = filterValue,
					ReportId = reportId,
					Type = ReportFilterType.FromValue(filterValue)
				};
				//test works
				//newFilter.SetValue("testarchived", "test" );
				var requiredInputs = newFilter.Type.Values();
				foreach (var dataType in requiredInputs)
				{
					var inputType = dataType.Value;
					Debug.WriteLine("here");
					switch (dataType.Value)
					{
						case InputType.BoardId:
							newFilter.SetValue(dataType.Key, dataIn.FirstOrDefault() );
							break;
						case InputType.ListingId:
							newFilter.SetValue(dataType.Key, dataIn.FirstOrDefault() );
							break;
						case InputType.Bool:
							Debug.WriteLine("here");
							newFilter.SetValue(dataType.Key, dataIn.Contains("archived") );
							break;
					}
				}
				
				string filterName = filterValue;
				var data = "";
/*
				switch (filterValue)
				{
					case "ByArchive":
						//DataBool = DataBool == "archived" ? "True" : "False";
						//data = DataBool;
						//filterName += $" - {DataBool}";

						break;
					case "ByBoard":

						//var board = await _context.Boards.FirstOrDefaultAsync(r => r.Id == DataId);

						if (board != null)
						{
						//	data = DataId.ToString();
							//filterName += $" - {board.Title}";
						}
						else
						{
							_notyf.Error($"Unable to add this filter. Cannot find Board.", 5);
							return RedirectToAction("EditReport", new { Id = reportId });
						}
						break;
					case "ByListing":

						//var listing = await _context.Listings.FirstOrDefaultAsync(r => r.Id == DataId);


						if (listing != null)
						{
							data = DataId.ToString();
							//filterName += $" - {listing.Title}";
						}
						else
						{
							_notyf.Error($"Unable to add this filter. Cannot find listing.", 5);
							return RedirectToAction("EditReport", new { Id = reportId });
						}
						break;
				}

				ReportFilter newFilter = new ReportFilter()
				{
					Name = filterName,
					ReportId = reportId,
					Type = ReportFilterType.FromValue(filterValue),
					Values = data
				};*/
				report.Filters.Add(newFilter);
				await _context.SaveChangesAsync();
			}
			ViewData["Filters"] = new SelectList(ReportFilterType.List, "Value", "Name");
			return RedirectToAction("EditReport", new { Id = reportId });
		}
		public async Task<IActionResult> RemoveFilter(int reportId, int filterId)
		{
			//Check if user is Admin.
			if (!UserIsAdmin(out IActionResult isAdmin)) return isAdmin;

			var reportFilter = await _context.ReportFilters.FirstOrDefaultAsync(f => f.Id == filterId);

			if (reportFilter == null)
				_notyf.Error($"Unable to remove this filter.", 5);
			else
			{
				try
				{
					_context.ReportFilters.Remove(reportFilter);
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

			}

			ViewData["Filters"] = new SelectList(ReportFilterType.List, "Value", "Name");
			return RedirectToAction("EditReport", new { Id = reportId });
		}
		public async Task<IActionResult> AddOperation(int reportId, string operationValue, string message, int? FieldId)
		{
			//Check if user is Admin.
			if (!UserIsAdmin(out IActionResult isAdmin)) return isAdmin;

			var report = await _context.Reports
				.Include(r => r.Operations)
				.FirstOrDefaultAsync(r => r.Id == reportId);

			if (report == null)
				_notyf.Error($"Unable to add this operation, report not found.", 5);
			else
			{
				try
				{

					var opType = ReportOperationType.FromValue(operationValue);

					if (report.Operations?.Any(op => op.Type == opType) == true)
					{
						_notyf.Error($"Unable to add this operation. Operation already exists on this report.", 5);
						return RedirectToAction("EditReport", new { Id = reportId });
					}


					string operationName = opType.Name;

					var values = "";

					object value = "";

					if (operationValue == "StaticMessage")
					{
						values = new KeyValuePair<string, string>("StaticMessage", message).ToJson();
						value = message;
					}
					else
					{
						if (operationValue == "Count")
						{
							values = new KeyValuePair<string, string>(operationValue, "Count").ToJson();
							value = "Count";
						}
						else
						{
							var field = await _context.CustomFields.FirstOrDefaultAsync(r => r.Id == FieldId);


							if (field != null)
							{
								values = new KeyValuePair<string, string>("CustomFieldId", FieldId.ToString()).ToJson();
								value = field;
							}
							else
							{
								_notyf.Error($"Unable to add this operation. Cannot find listing.", 5);
								return RedirectToAction("EditReport", new { Id = reportId });
							}
						}
					}

					ReportOperation newOperation = new ReportOperation()
					{
						Name = operationName,
						ReportId = reportId,
						Type = ReportOperationType.FromValue(operationValue),
						Values = values
					};

					newOperation.SetValue("CustomFieldId", value);

					//ReportOperation newOperation = new ReportOperation()
					//{
					//	Name = operationName,
					//	ReportId = reportId,
					//	Type = ReportOperationType.FromValue(operationValue),
					//	Values = data
					//};

					report.Operations.Add(newOperation);
					await _context.SaveChangesAsync();
				}
				catch
				{
					_notyf.Error($"Error while attempting to add Operation. Please try again or contact an administrator.", 5);
				}
			}

			return RedirectToAction("EditReport", new { Id = reportId });
		}

		public async Task<IActionResult> RemoveOperation(int reportId, int operationId)
		{
			//Check if user is Admin.
			if (!UserIsAdmin(out IActionResult isAdmin)) return isAdmin;

			var reportOp = await _context.ReportOperations.FirstOrDefaultAsync(f => f.Id == operationId);

			if (reportOp == null)
				_notyf.Error($"Unable to remove this operation.", 5);
			else
			{
				try
				{
					_context.ReportOperations.Remove(reportOp);
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
			}
			ViewData["Operations"] = new SelectList(ReportOperationType.List, "Value", "Name");
			return RedirectToAction("EditReport", new { Id = reportId });
		}

		public async Task<IActionResult> RunReport(int reportId)
		{
			//Check if user is Admin.
			if (!UserIsAdmin(out IActionResult isAdmin)) return isAdmin;

			var report = await _context.Reports
				.Include(r => r.Filters)
				.Include(r => r.Operations)
				.Include(r => r.Results)
				.FirstOrDefaultAsync(r => r.Id == reportId);


			if (report == null || report?.Operations.Count == 0)
				_notyf.Error($"Unable to run report. Report either not found, or contains no operations to run.", 10);
			else
			{
				try
				{
					var cards = _context.Cards.AsQueryable();

					foreach (var filter in report.Filters)
					{
						//need to store the results that are returned
						cards = filter.Apply(cards);
					}

					Dictionary<string, string> ReportResults = new Dictionary<string, string>();

					foreach (var operation in report.Operations)
					{
						ReportResults.Add(new KeyValuePair<string, string>(operation.Name, operation.Result(cards)));
					}

					ReportResult result = new ReportResult()
					{
						ReportId = reportId,
						Created = DateTime.Now,
						Result = ReportResults.ToJson()
					};
					report.Results.Add(result);


					await _context.SaveChangesAsync();
					ViewData["Operations"] = new SelectList(ReportOperationType.List, "Value", "Name");
				}
				catch
				{
					_notyf.Error($"Unable to run report due to error. Please try again or contact an administrator.", 5);
				}
			}

			return RedirectToAction("DetailsReport", new { Id = reportId });
		}

		public async Task<IActionResult> ReportResults(int reportId, int reportResultId)
		{
			//Check if user is Admin.
			if (!UserIsAdmin(out IActionResult isAdmin)) return isAdmin;

			//Get return URL
			ViewDataReturnURL();

			var reportResult = await _context.ReportResults
				.FirstOrDefaultAsync(r => r.Id == reportResultId);


			if (reportResult == null)
				_notyf.Error($"Unable to find report results.", 5);
			else
			{

				var report = await _context.Reports.FirstOrDefaultAsync(r => r.Id == reportId);

				if (report != null)
				{
					ViewBag.Report = report;
					return View(reportResult);
				}
				else
				{
					_notyf.Error($"Unable to find parent of report results.", 5);
				}
			}

			return RedirectToAction("DetailsReport", new { Id = reportId });
		}
		public async Task<IActionResult> RemoveResult(int reportId, int reportResultId)
		{
			//Check if user is Admin.
			if (!UserIsAdmin(out IActionResult isAdmin)) return isAdmin;

			var reportResult = await _context.ReportResults.FirstOrDefaultAsync(f => f.Id == reportResultId);

			if (reportResult == null)
				_notyf.Error($"Unable to remove this result record.", 5);
			else
			{
				try
				{
					_context.ReportResults.Remove(reportResult);
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
			}

			return RedirectToAction("DetailsReport", new { Id = reportId });
		}


		private void ViewDataReturnURL()
		{
			ViewData["returnURL"] = MaintainURL.ReturnURL(HttpContext, ControllerName());
		}

		private bool UserIsAdmin(out IActionResult redirect)
		{

			try
			{
				var user = GoatAuthorize.CreateFromContext(HttpContext).Result;
				//Raise an error if the user doesn't have admin permission for this board
				if (!user.IsSiteAdmin || user.IsArchived)
				{
					_notyf.Error($"Permission Denied.", 5);
					redirect = RedirectToAction("Index", "Home");
					return false;
				}
				else
				{
					redirect = null;
					return true;
				}
			}
			catch
			{
				_notyf.Error($"Permission Denied.", 5);
				redirect = RedirectToAction("Index", "Home");
				return false;
			}
		}

		private void PopulateDropdowns()
		{

			ViewData["Filters"] = new SelectList(ReportFilterType.List, "Value", "Name");
			ViewData["FilterBoards"] = new SelectList(_context.Boards, "Id", "Title");

			List<SelectListItem> Listings = new List<SelectListItem>()
				{
					new SelectListItem() { Text="Select A Listing", Disabled=true, Selected=true },
				};

			foreach (var board in _context.Boards.Include(b => b.Listings))
			{
				Listings.Add(new SelectListItem() { Text = $"---{board.Title}---", Disabled = true });

				foreach (var listing in board.Listings.Where(l => !l.IsTemplate))
				{
					Listings.Add(new SelectListItem() { Text = listing.Title, Value = listing.Id.ToString() });
				}
			}

			ViewData["FilterListings"] = Listings;

			ViewData["Operations"] = new SelectList(ReportOperationType.List, "Value", "Name");

			ViewData["CustomFields"] = new SelectList(_context.CustomFields, "Id", "Name");
		}
	}
}
