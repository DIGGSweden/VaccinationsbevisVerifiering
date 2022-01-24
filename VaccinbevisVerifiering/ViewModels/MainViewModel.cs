﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Windows.Input;
using VaccinbevisVerifiering.Models;
using VaccinbevisVerifiering.Resources;
using VaccinbevisVerifiering.Services;
using VaccinbevisVerifiering.Services.DGC;
using VaccinbevisVerifiering.Services.DGC.V1;
using VaccinbevisVerifiering.Views;
using Xamarin.Essentials;
using Xamarin.Forms;

namespace VaccinbevisVerifiering.ViewModels
{
    public class MainViewModel : BaseViewModel
    {

        private ICommand scanCommand;
        private ICommand aboutCommand;
        private string _validKeysText;
        public static CertificateManager CertificateManager { get; private set; }
        public MainViewModel()
        {
            CertificateManager = new CertificateManager(new RestService());

            MessagingCenter.Subscribe<Xamarin.Forms.Application>(Xamarin.Forms.Application.Current, "Cancel", async (sender) =>
            {
                try
                {
                    await Application.Current.MainPage.Navigation.PopModalAsync();
                }
                catch (Exception ex)
                { }

            });
            MessagingCenter.Subscribe<Xamarin.Forms.Application>(Xamarin.Forms.Application.Current, "Scan", async (sender) =>
            {
                try
                {
                    await Application.Current.MainPage.Navigation.PopModalAsync();
                }
                catch (Exception ex)
                { }
                await Scan();
            });

            MessagingCenter.Subscribe<Xamarin.Forms.Application>(Xamarin.Forms.Application.Current, "PublicKeysUpdated", async (sender) =>
            {
                try
                {

                    ValidateKeys();
                }
                catch (Exception ex)
                { }
            });

            ValidateKeys();
        }

        private void ValidateKeys()
        {
            if (App.CertificateManager.TrustList == null)
            {
                ValidKeysText = AppResources.NoPublicKeys;
            }
            else if ((App.CertificateManager.TrustList.Iat + 172800) < App.CertificateManager.GetSecondsFromEpoc())
            {
                // warn if downloaded trustlist is older than 48h
                ValidKeysText = AppResources.OldPublicKeys;
            }
            else
            {
                ValidKeysText = null;
            }

            if (ValidKeysText == AppResources.NoPublicKeys || ValidKeysText == AppResources.OldPublicKeys ||
                ValidKeysText == AppResources.UpdatePublicKeys)
            {
                MessagingCenter.Send(Application.Current, "DisplayPublicKeysError");
            }
        }
        public ICommand TapCommand => new Command<string>(async (url) => await Launcher.OpenAsync(url));

        public ICommand AboutCommand => aboutCommand ??
                (aboutCommand = new Command(async () =>
                {
                    CertificateManager.LoadCertificates();
                    CertificateManager.LoadValueSets();
                    await CertificateManager.LoadVaccineRules();
                    await Application.Current.MainPage.Navigation.PushAsync(new AboutPage());
                }));

        public ICommand ScanCommand
        {

            get
            {
                return scanCommand ??
                (scanCommand = new Command(async () => await Scan()));
            }
        }

        public async Task Scan() {

            try
            {
                var scanner = DependencyService.Get<IQRScanningService>();
                var result = await scanner.ScanAsync();

                if( result !=null)
                {
                    ResultViewModel resultModel = new ResultViewModel();
                    resultModel.UpdateFields(result);
                    ResultPage resultPage = new ResultPage();
                    resultPage.BindingContext = resultModel;
                    await Application.Current.MainPage.Navigation.PushModalAsync(resultPage);
                }
            }
            catch (Exception ex)
            {
            }
        }

        public String ValidKeysText
        {
            get { return _validKeysText; }
            set
            {
                _validKeysText = value;
                OnPropertyChanged();
                OnPropertyChanged("ValidKeysTextVisible");
            }
        }

        public bool ValidKeysTextVisible
        {
            get { return (_validKeysText==null?false:true); }
        }

    }

    
}