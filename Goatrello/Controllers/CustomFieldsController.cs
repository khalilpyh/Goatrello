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
using Goatrello.Services;
using AspNetCoreHero.ToastNotification.Abstractions;

namespace Goatrello.Controllers
{
    [Authorize]
    public class CustomFieldsController : CognizantController
    {
        private readonly GoatrelloDataContext _context;
        //for toast notification dependency injection
        private readonly INotyfService _notyf;

        public CustomFieldsController(GoatrelloDataContext context, INotyfService notyf)
        {
            _context = context;
            _notyf = notyf;
        }

        // GET: CustomFields
        public async Task<IActionResult> Index()
        {
              return View(await _context.CustomFields.ToListAsync());
        }

        // GET: CustomFields/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null || _context.CustomFields == null)
            {
                return NotFound();
            }

            var customField = await _context.CustomFields
                .FirstOrDefaultAsync(m => m.Id == id);
            if (customField == null)
            {
                return NotFound();
            }

            return View(customField);
        }

        // GET: CustomFields/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: CustomFields/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Id,Name,FieldDataType")] CustomField customField)
        {
            if (ModelState.IsValid)
            {
                _context.Add(customField);
                await _context.SaveChangesAsync();

				//notification for create
				if (customField.Name.Length > 10)
					_notyf.Custom($"Field {customField.Name.Substring(0, 10)}... is created!", 5, "#602AC3", "fa fa-check");
				else
					_notyf.Custom($"Field {customField.Name} is created!", 5, "#602AC3", "fa fa-check");

				return RedirectToAction(nameof(Index));
            }
            return View(customField);
        }

        // GET: CustomFields/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null || _context.CustomFields == null)
            {
                return NotFound();
            }

            var customField = await _context.CustomFields.FindAsync(id);
            if (customField == null)
            {
                return NotFound();
            }
            return View(customField);
        }

        // POST: CustomFields/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Name,FieldDataType")] CustomField customField)
        {
            if (id != customField.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(customField);
                    await _context.SaveChangesAsync();

					//notification for edit
					if (customField.Name.Length > 10)
						_notyf.Custom($"Field {customField.Name.Substring(0, 10)}... is updated!", 5, "#602AC3", "fa fa-refresh");
					else
						_notyf.Custom($"Field {customField.Name} is updated!", 5, "#602AC3", "fa fa-refresh");
				}
                catch (DbUpdateConcurrencyException)
                {
                    if (!CustomFieldExists(customField.Id))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(Index));
            }
            return View(customField);
        }

        // GET: CustomFields/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null || _context.CustomFields == null)
            {
                return NotFound();
            }

            var customField = await _context.CustomFields
                .FirstOrDefaultAsync(m => m.Id == id);
            if (customField == null)
            {
                return NotFound();
            }

            return View(customField);
        }

        // POST: CustomFields/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            if (_context.CustomFields == null)
            {
                return Problem("Entity set 'GoatrelloDataContext.CustomFields'  is null.");
            }
            var customField = await _context.CustomFields.FindAsync(id);
            if (customField != null)
            {
                _context.CustomFields.Remove(customField);
            }
            
            await _context.SaveChangesAsync();

			//notification for delete
			if (customField.Name.Length > 10)
				_notyf.Custom($"Field {customField.Name.Substring(0, 10)}... has been archived!", 5, "#495867", "fa fa-trash");
			else
				_notyf.Custom($"Field {customField.Name} has been archived!", 5, "#495867", "fa fa-trash");

			return RedirectToAction(nameof(Index));
        }

        private bool CustomFieldExists(int id)
        {
          return _context.CustomFields.Any(e => e.Id == id);
        }
    }
}
