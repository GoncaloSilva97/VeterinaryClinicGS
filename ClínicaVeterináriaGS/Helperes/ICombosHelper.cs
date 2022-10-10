﻿using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace VeterinaryClinicGS.Helperes
{
    public interface ICombosHelper
    {
        IEnumerable<SelectListItem> GetComboPetTypes();

        IEnumerable<SelectListItem> GetComboServiceTypes();

        IEnumerable<SelectListItem> GetComboOwners();

        IEnumerable<SelectListItem> GetComboPets(int ownerId);
    }
}