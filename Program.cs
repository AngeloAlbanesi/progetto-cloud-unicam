using FunzioniDatiHotell;
using ProvinciaTrentoHotel.WSSoap;
using SoapCore;

namespace ProvinciaTrentoHotel
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Add services to the container.

            builder.Services.AddSoapCore();
            builder.Services.AddScoped<
                IServizioHotelProvinciaTrento,
                ServizioHotelProvinciaTrento
            >();

            builder.Services.AddControllersWithViews();

            var app = builder.Build();

            // Recupero la configurazione DatiOpen e imposto l'URL base
            var hotelApiBaseUri = app.Configuration.GetValue<string>("DatiOpen:HotelApiBaseUri");
            if (!string.IsNullOrEmpty(hotelApiBaseUri))
            {
                FunzioneInterrogazioneDati.SetBaseUrl(hotelApiBaseUri);
            }
            else
            {
                throw new InvalidOperationException(
                    "La configurazione DatiOpen:HotelApiBaseUri non è stata trovata in appsettings.json"
                );
            }

            // Configure the HTTP request pipeline.
            if (!app.Environment.IsDevelopment())
            {
                app.UseExceptionHandler("/Home/Error");
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts();
            }

            app.UseHttpsRedirection();
            app.UseStaticFiles();

            app.UseRouting();

            app.UseAuthorization();

            app.MapControllerRoute(
                name: "default",
                pattern: "{controller=Home}/{action=Index}/{id?}"
            );


            //configurazione endpoint per servizio SOAP
            app.UseEndpoints(endpoints =>
            {
                endpoints.UseSoapEndpoint<IServizioHotelProvinciaTrento>(
                    "/ServizioHotelProvinciaTrento.wsdl",
                    new SoapEncoderOptions(),
                    SoapSerializer.XmlSerializer
                );
            });
            app.Run();
        }
    }
}
