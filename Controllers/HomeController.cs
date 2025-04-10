using System.Diagnostics;
using FunzioniDatiHotell;
using FunzioniDatiHotell.Modelli;
using Microsoft.AspNetCore.Mvc;
using ProvinciaTrentoHotel.ViewModels;

namespace ProvinciaTrentoHotel.Controllers
{
    public class HomeController : Controller
    {
        public ActionResult Index()
        {
            return View();
        }

        public IActionResult ElencoHotel()
        {
            HomeElencoHotelViewModel vm = new HomeElencoHotelViewModel();
            Hotel[] ElencoHotel = FunzioneInterrogazioneDati.DaiElencoHotel().Result;
            vm.ElencoHotel = ElencoHotel;
            return View(vm);
        }

        [HttpPost]
        public IActionResult RicercaHotelComune(string InputComune)
        {
            HomeRicercaHotelComuneViewModel vmRicercaComune = new HomeRicercaHotelComuneViewModel();
            Hotel[] RicercaHotel = FunzioneInterrogazioneDati
                .RicercaHotelPerComune(InputComune)
                .Result;
            vmRicercaComune.ComuniDisponibili = FunzioneInterrogazioneDati
                .ComuniDisponibili()
                .Result;
            vmRicercaComune.RicercaHotel = RicercaHotel;
            return View(vmRicercaComune);
        }

        public IActionResult RicercaHotelComune()
        {
            HomeRicercaHotelComuneViewModel vmRicercaComune = new HomeRicercaHotelComuneViewModel();
            vmRicercaComune.ComuniDisponibili = FunzioneInterrogazioneDati
                .ComuniDisponibili()
                .Result; //necessario per menu a tendina
            return View(vmRicercaComune);
        }

        public IActionResult DettagliHotel(string pIva)
        {
            HomeDettagliHotelViewModel vmDettagliHotel = new HomeDettagliHotelViewModel();
            Hotel hotelDettagliato = FunzioneInterrogazioneDati.RicercaHotelPerPIVA(pIva).Result;
            vmDettagliHotel.hotelDettagliato = hotelDettagliato;
            return View(vmDettagliHotel);
        }
    }
}
