﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using VeterinaryClinicGS.Data;
using VeterinaryClinicGS.Data.Entity;
using VeterinaryClinicGS.Helperes;

using VeterinaryClinicGS.Models;

namespace VeterinaryClinicGS.Controllers
{
    [Authorize(Roles = "Admin")]
    public class OwnersController : Controller
    {
        private readonly IOwnerRepository _ownerRepository;
        private readonly DataContext _dataContext;
        private readonly IUserHelper _userHelper;
        private readonly ICombosHelper _combosHelper;
        private readonly IConverterHelper _converterHelper;
        private readonly IImageHelper _imageHelper;
        private readonly IBlobHelper _blobHelper;

        public OwnersController(
            IOwnerRepository ownerRepository,
            DataContext context,
            IUserHelper userHelper,
            ICombosHelper combosHelper,
            IConverterHelper converterHelper,
            IImageHelper imageHelper,
            IBlobHelper blobHelper)
        {
            _ownerRepository = ownerRepository;
            _dataContext = context;
            _userHelper = userHelper;
            _combosHelper = combosHelper;
            _converterHelper = converterHelper;
            _imageHelper = imageHelper;
            _blobHelper = blobHelper;
        }

        public IActionResult Index()
        {
            return View(_dataContext.Owners
                .Include(o => o.User)
                .Include(o => o.Animal));
        }

        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var owner = await _dataContext.Owners
                .Include(o => o.User)
                .Include(o => o.Animal)
                .ThenInclude(p => p.AnimalType)
                .Include(o => o.Animal)
                .ThenInclude(p => p.Histories)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (owner == null)
            {
                return NotFound();
            }

            return View(owner);
        }

        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(AddUserViewModel model)
        {
            if (ModelState.IsValid)
            {
                var user = new User
                {
                    Address = model.Address,
                    Document = model.Document,
                    Email = model.Username,
                    FirstName = model.FirstName,
                    LastName = model.LastName,
                    PhoneNumber = model.PhoneNumber,
                    UserName = model.Username
                };

                var response = await _userHelper.AddUserAsync(user, model.Password);
                if (response.Succeeded)
                {
                    var userInDB = await _userHelper.GetUserByEmailAsync(model.Username);
                    await _userHelper.AddUserToRoleAsync(userInDB, "Customer");

                    var owner = new Owners
                    {
                        Agendas = new List<Agenda>(),
                        Animal = new List<Animals>(),
                        User = userInDB
                    };

                    _dataContext.Owners.Add(owner);

                    try
                    {
                        await _dataContext.SaveChangesAsync();

                       

                        return RedirectToAction(nameof(Index));
                    }
                    catch (Exception ex)
                    {
                        ModelState.AddModelError(string.Empty, ex.ToString());
                        return View(model);
                    }
                }

                ModelState.AddModelError(string.Empty, response.Errors.FirstOrDefault().Description);
            }

            return View(model);
        }

        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var owner = await _dataContext.Owners
                .Include(o => o.User)
                .FirstOrDefaultAsync(o => o.Id == id.Value);
            if (owner == null)
            {
                return NotFound();
            }

            var model = new ChangeUserViewModel
            {
                Address = owner.User.Address,
                Document = owner.User.Document,
                FirstName = owner.User.FirstName,
                Id = owner.Id,
                LastName = owner.User.LastName,
                PhoneNumber = owner.User.PhoneNumber
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(ChangeUserViewModel model)
        {
            if (ModelState.IsValid)
            {
                var owner = await _dataContext.Owners
                    .Include(o => o.User)
                    .FirstOrDefaultAsync(o => o.Id == model.Id);

                owner.User.Document = model.Document;
                owner.User.FirstName = model.FirstName;
                owner.User.LastName = model.LastName;
                owner.User.Address = model.Address;
                owner.User.PhoneNumber = model.PhoneNumber;

                await _userHelper.UpdateUserAsync(owner.User);
                return RedirectToAction(nameof(Index));
            }

            return View(model);
        }

        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var owner = await _dataContext.Owners
                .Include(o => o.User)
                .Include(o => o.Animal)
                .FirstOrDefaultAsync(o => o.Id == id);
            if (owner == null)
            {
                return NotFound();
            }

            if (owner.Animal.Count > 0)
            {
                //TODO: Message
                return RedirectToAction(nameof(Index));
            }

            await _userHelper.DeletAsync(owner.User.Email);
            _dataContext.Owners.Remove(owner);
            await _dataContext.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool OwnerExists(int id)
        {
            return _dataContext.Owners.Any(e => e.Id == id);
        }

        public async Task<IActionResult> AddAnimal(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var owner = await _dataContext.Owners.FindAsync(id.Value);
            if (owner == null)
            {
                return NotFound();
            }

            var model = new AnimalViewModel
            {
                Born = DateTime.Today,
                OwnerId = owner.Id,
                AnimalTypes = _combosHelper.GetComboPetTypes()
            };

            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> AddAnimal(AnimalViewModel model)
        {
            if (ModelState.IsValid)
            {

                Guid imageId = model.ImageId;
                if (model.ImageFile != null && model.ImageFile.Length > 0)
                {
                    imageId = await _blobHelper.UploadBlobAsync(model.ImageFile, "foto");

                }
                //var path = string.Empty;

                //if (model.ImageFile != null)
                //{
                //    path = await _imageHelper.UploadImageAsync(model.ImageFile);
                //}

                var Animal = await _converterHelper.ToAnimalAsync(model, imageId, true);
                _dataContext.Animals.Add(Animal);
                await _dataContext.SaveChangesAsync();
                return RedirectToAction($"Details/{model.OwnerId}");
            }

            model.AnimalTypes = _combosHelper.GetComboPetTypes();
            return View(model);
        }

        public async Task<IActionResult> EditAnimal(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var animal = await _dataContext.Animals
                .Include(p => p.Owner)
                .Include(p => p.AnimalType)
                .FirstOrDefaultAsync(p => p.Id == id);
            if (animal == null)
            {
                return NotFound();
            }

            return View(_converterHelper.ToAnimalViewModel(animal));
        }

        [HttpPost]
        public async Task<IActionResult> EditAnimal(AnimalViewModel model)
        {
            if (ModelState.IsValid)
            {
                Guid imageId = model.ImageId;
                if (model.ImageFile != null && model.ImageFile.Length > 0)
                {
                    imageId = await _blobHelper.UploadBlobAsync(model.ImageFile, "foto");

                }

                //if (model.ImageFile != null)
                //{
                //    path = await _imageHelper.UploadImageAsync(model.ImageFile);
                //}

                var animal = await _converterHelper.ToAnimalAsync(model, imageId, false);
                _dataContext.Animals.Update(animal);
                await _dataContext.SaveChangesAsync();
                return RedirectToAction($"Details/{model.OwnerId}");
            }

            model.AnimalTypes = _combosHelper.GetComboPetTypes();
            return View(model);
        }

        public async Task<IActionResult> DetailsAnimal(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var animal = await _dataContext.Animals
                .Include(p => p.Owner)
                .ThenInclude(o => o.User)
                .Include(p => p.Histories)
                .ThenInclude(h => h.ServiceType)
                .FirstOrDefaultAsync(o => o.Id == id.Value);
            if (animal == null)
            {
                return NotFound();
            }

            return View(animal);
        }

        public async Task<IActionResult> AddHistory(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var animal = await _dataContext.Animals.FindAsync(id.Value);
            if (animal == null)
            {
                return NotFound();
            }

            var model = new HistoryViewModel
            {
                Date = DateTime.Now,
                AnimalId = animal.Id,
                ServiceTypes = _combosHelper.GetComboServiceTypes(),
            };

            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> AddHistory(HistoryViewModel model)
        {
            if (ModelState.IsValid)
            {
                var history = await _converterHelper.ToHistoryAsync(model, true);
                _dataContext.Histories.Add(history);
                await _dataContext.SaveChangesAsync();
                return RedirectToAction($"{nameof(DetailsAnimal)}/{model.AnimalId}");
            }

            model.ServiceTypes = _combosHelper.GetComboServiceTypes();
            return View(model);
        }

        public async Task<IActionResult> EditHistory(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var history = await _dataContext.Histories
                .Include(h => h.Animal)
                .Include(h => h.ServiceType)
                .FirstOrDefaultAsync(p => p.Id == id.Value);
            if (history == null)
            {
                return NotFound();
            }

            return View(_converterHelper.ToHistoryViewModel(history));
        }

        [HttpPost]
        public async Task<IActionResult> EditHistory(HistoryViewModel model)
        {
            if (ModelState.IsValid)
            {
                var history = await _converterHelper.ToHistoryAsync(model, false);
                _dataContext.Histories.Update(history);
                await _dataContext.SaveChangesAsync();
                return RedirectToAction($"{nameof(DetailsAnimal)}/{model.AnimalId}");
            }

            model.ServiceTypes = _combosHelper.GetComboServiceTypes();
            return View(model);
        }

        public async Task<IActionResult> DeleteHistory(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var history = await _dataContext.Histories
                .Include(h => h.Animal)
                .FirstOrDefaultAsync(h => h.Id == id.Value);
            if (history == null)
            {
                return NotFound();
            }

            _dataContext.Histories.Remove(history);
            await _dataContext.SaveChangesAsync();
            return RedirectToAction($"{nameof(DetailsAnimal)}/{history.Animal.Id}");
        }

        public async Task<IActionResult> DeletePet(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var animal = await _dataContext.Animals
                .Include(p => p.Owner)
                .Include(p => p.Histories)
                .FirstOrDefaultAsync(p => p.Id == id.Value);
            if (animal == null)
            {
                return NotFound();
            }

            if(animal.Histories.Count > 0)
            {
                ModelState.AddModelError(string.Empty, "The pet can't be deleted because it has related records.");
                return RedirectToAction($"{nameof(Details)}/{animal.Owner.Id}");
            }

            _dataContext.Animals.Remove(animal);
            await _dataContext.SaveChangesAsync();
            return RedirectToAction($"{nameof(Details)}/{animal.Owner.Id}");
        }
    }
}
