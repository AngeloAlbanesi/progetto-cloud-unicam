using System.Diagnostics;
using FunzioniDatiHotell;
using FunzioniDatiHotell.Modelli;
using Microsoft.AspNetCore.Mvc;
using ProvinciaTrentoHotel.ViewModels;

namespace ProvinciaTrentoHotel.Controllers
{
    //PROVA
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
