﻿using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Mvc.Rendering;
using VeterinaryClinicGS.Data;

namespace VeterinaryClinicGS.Helperes
{
    public class CombosHelper : ICombosHelper
    {
        private readonly DataContext _dataContext;

        public CombosHelper(DataContext dataContext)
        {
            _dataContext = dataContext;
        }

        public IEnumerable<SelectListItem> GetComboPetTypes()
        {
            var list = _dataContext.AnimalTypes.Select(pt => new SelectListItem
            {
                Text = pt.Name,
                Value = $"{pt.Id}"
            })
                .OrderBy(pt => pt.Text)
                .ToList();

            list.Insert(0, new SelectListItem
            {
                Text = "[Select a pet type...]",
                Value = "0"
            });

            return list;
        }

        public IEnumerable<SelectListItem> GetComboServiceTypes()
        {
            var list = _dataContext.ServiceTypes.Select(pt => new SelectListItem
            {
                Text = pt.Name,
                Value = $"{pt.Id}"
            })
                .OrderBy(pt => pt.Text)
                .ToList();

            list.Insert(0, new SelectListItem
            {
                Text = "[Select a service type...]",
                Value = "0"
            });

            return list;
        }

        public IEnumerable<SelectListItem> GetComboOwners()
        {
            var list = _dataContext.Owners.Select(p => new SelectListItem
            {
                Text = p.User.FullName,
                Value = p.Id.ToString()
            }).OrderBy(p => p.Text).ToList();

            list.Insert(0, new SelectListItem
            {
                Text = "(Select an owner...)",
                Value = "0"
            });

            return list;
        }

        public IEnumerable<SelectListItem> GetComboPets(int ownerId)
        {
            var list = _dataContext.Animals.Where(p => p.Owner.Id == ownerId).Select(p => new SelectListItem
            {
                Text = p.Name,
                Value = p.Id.ToString()
            }).OrderBy(p => p.Text).ToList();

            list.Insert(0, new SelectListItem
            {
                Text = "(Select a pet...)",
                Value = "0"
            });

            return list;
        }

    }
}
