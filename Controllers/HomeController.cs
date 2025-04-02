using FunzioniDatiHotel;
using FunzioniDatiHotel.Modelli;

using Microsoft.AspNetCore.Mvc;

using ProvinciaTrentoHotel.ViewModels;
using System.Diagnostics;

namespace ProvinciaTrentoHotel.Controllers
{
    public class HomeController : Controller
    {
        

        public IActionResult ElencoHotel()
        {
            HomeElencoHotelViewModel vm = new HomeElencoHotelViewModel();
            Hotel[] ElencoHotel = FunzioneInterrogazioneDati.DaiElencoHotel().Result;
            vm.ElencoHotel = ElencoHotel;
            return View(vm);

        }

       
    }
}
