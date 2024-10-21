// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Turnstile.Core.Interfaces;
using Turnstile.Core.Models.Configuration;
using Turnstile.Web.Admin.Models.PublisherConfig;
using Turnstile.Web.Common.Models;
using Turnstile.Web.Common.Extensions;
using Turnstile.Web.Admin.Extensions;
using Newtonsoft.Json;

namespace Turnstile.Web.Admin.Controllers
{
    [Authorize]
    public class PublisherConfigController : Controller
    {
        public static class RouteNames
        {
            public const string ConfigureBasics = "configure_basics";
            public const string ConfigureClaims = "configure_claims";
            public const string ConfigureSeatingStrategy = "configure_seating_strategy";
            public const string ConfigureUserRedirection = "configure_redirection";
            public const string ConfigureMonaIntegration = "configure_mona";
        }

        private readonly ILogger logger;
        private readonly IPublisherConfigurationClient publisherConfigClient;

        public PublisherConfigController(
            ILogger<PublisherConfigController> logger,
            IPublisherConfigurationClient publisherConfigClient)
        {
            this.logger = logger;
            this.publisherConfigClient = publisherConfigClient;
        }

        [HttpGet, Route("config/basics", Name = RouteNames.ConfigureBasics)]
        public async Task<IActionResult> ConfigureBasics()
        {
            try
            {
                var publisherConfig = await publisherConfigClient.GetConfiguration();

                if (publisherConfig != null)
                {
                    this.ApplyModel(new LayoutViewModel(publisherConfig!));
                }

                return View(new BasicConfigurationViewModel(publisherConfig!));
            }
            catch (Exception ex)
            {
                logger.LogError($"Exception @ GET [{nameof(ConfigureBasics)}]: [{ex.Message}].");

                throw;
            }
        }

        [HttpGet, Route("config/claims", Name = RouteNames.ConfigureClaims)]
        public async Task<IActionResult> ConfigureClaims()
        {
            try
            {
                var publisherConfig = await publisherConfigClient.GetConfiguration();

                if (publisherConfig?.IsSetupComplete == true)
                {
                    this.ApplyModel(new LayoutViewModel(publisherConfig!));

                    return View(new ClaimsConfigurationViewModel(publisherConfig!.ClaimsConfiguration));
                }
                else
                {
                    return RedirectToBasics();
                }
            }
            catch (Exception ex)
            {
                logger.LogError($"Exception @ GET [{nameof(ConfigureClaims)}]: [{ex.Message}].");

                throw;
            }
        }

        [HttpPost, Route("config/basics")]
        public async Task<IActionResult> ConfigureBasics([FromForm] BasicConfigurationViewModel basicConfig)
        {
            try
            {
                var publisherConfig = await publisherConfigClient.GetConfiguration() ?? new PublisherConfiguration();

                if (ModelState.IsValid)
                {
                    publisherConfig!.Apply(basicConfig);

                    publisherConfig!.IsSetupComplete = true; // The basics are all we need to get started.

                    await publisherConfigClient.UpdateConfiguration(publisherConfig!);

                    basicConfig.IsConfigurationSaved = true;
                    basicConfig.HasValidationErrors = false;
                }
                else
                {
                    basicConfig.IsConfigurationSaved = false;
                    basicConfig.HasValidationErrors = true;
                }

                this.ApplyModel(new LayoutViewModel(publisherConfig));

                return View(basicConfig);
            }
            catch (Exception ex)
            {
                logger.LogError($"Exception @ POST [{nameof(ConfigureBasics)}: [{ex.Message}]");

                throw;
            }
        }

        [HttpPost, Route("config/claims")]
        public async Task<IActionResult> ConfigureClaims([FromForm] ClaimsConfigurationViewModel claimsConfigModel)
        {
            try
            {
                var publisherConfig = await publisherConfigClient.GetConfiguration();

                if (publisherConfig?.IsSetupComplete is true)
                {
                    if (ModelState.IsValid)
                    {
                        publisherConfig!.Apply(claimsConfigModel);

                        await publisherConfigClient.UpdateConfiguration(publisherConfig!);

                        claimsConfigModel.IsConfigurationSaved = true;
                        claimsConfigModel.HasValidationErrors = false;
                    }
                    else
                    {
                        claimsConfigModel.IsConfigurationSaved = false;
                        claimsConfigModel.HasValidationErrors = true;
                    }

                    this.ApplyModel(new LayoutViewModel(publisherConfig));

                    return View(claimsConfigModel);
                }
                else
                {
                    return RedirectToBasics();
                }
            }
            catch (Exception ex)
            {
                logger.LogError($"Exception @ POST [{nameof(ConfigureClaims)}: [{ex.Message}]");

                throw;
            }
        }

        [HttpGet, Route("config/redirection", Name = RouteNames.ConfigureUserRedirection)]
        public async Task<IActionResult> ConfigureUserRedirection()
        {
            try
            {
                var publisherConfig = await publisherConfigClient.GetConfiguration();

                if (publisherConfig?.IsSetupComplete == true)
                {
                    this.ApplyModel(new LayoutViewModel(publisherConfig!));

                    return View(new RedirectConfigurationViewModel(publisherConfig!));
                }
                else
                {
                    return RedirectToBasics();
                }
            }
            catch (Exception ex)
            {
                logger.LogError($"Exception @ GET [{nameof(ConfigureUserRedirection)}]: [{ex.Message}].");

                throw;
            }
        }

        [HttpPost, Route("config/redirection")]
        public async Task<IActionResult> ConfigureUserRedirection([FromForm] RedirectConfigurationViewModel redirectConfig)
        {
            try
            {
                var publisherConfig = await publisherConfigClient.GetConfiguration();

                if (publisherConfig?.IsSetupComplete == true)
                {
                    if (ModelState.IsValid)
                    {
                        publisherConfig!.Apply(redirectConfig);

                        await publisherConfigClient.UpdateConfiguration(publisherConfig!);

                        redirectConfig.IsConfigurationSaved = true;
                        redirectConfig.HasValidationErrors = false;
                    }
                    else
                    {
                        redirectConfig.IsConfigurationSaved = false;
                        redirectConfig.HasValidationErrors = true;
                    }

                    this.ApplyModel(new LayoutViewModel(publisherConfig!));

                    return View(redirectConfig);
                }
                else
                {
                    return RedirectToBasics();
                }
            }
            catch (Exception ex)
            {
                logger.LogError($"Exception @ POST [{nameof(ConfigureUserRedirection)}: [{ex.Message}]");

                throw;
            }
        }

        [HttpGet, Route("config/mona", Name = RouteNames.ConfigureMonaIntegration)]
        public async Task<IActionResult> ConfigureMonaIntegration()
        {
            try
            {
                var publisherConfig = await publisherConfigClient.GetConfiguration();

                if (publisherConfig?.IsSetupComplete == true)
                {
                    this.ApplyModel(new LayoutViewModel(publisherConfig!));

                    return View(new MonaConfigurationViewModel(publisherConfig!));
                }
                else
                {
                    return RedirectToBasics();
                }
            }
            catch (Exception ex)
            {
                logger.LogError($"Exception @ GET [{nameof(ConfigureMonaIntegration)}]: [{ex.Message}].");

                throw;
            }
        }


        [HttpPost, Route("config/mona")]
        public async Task<IActionResult> ConfigureMonaIntegration([FromForm] MonaConfigurationViewModel monaConfig)
        {
            try
            {
                var publisherConfig = await publisherConfigClient.GetConfiguration();

                if (publisherConfig?.IsSetupComplete == true)
                {
                    if (ModelState.IsValid)
                    {
                        publisherConfig!.Apply(monaConfig);

                        await publisherConfigClient.UpdateConfiguration(publisherConfig!);

                        monaConfig.IsConfigurationSaved = true;
                        monaConfig.HasValidationErrors = false;
                    }
                    else
                    {
                        monaConfig.IsConfigurationSaved = false;
                        monaConfig.HasValidationErrors = true;
                    }

                    this.ApplyModel(new LayoutViewModel(publisherConfig));

                    return View(monaConfig);
                }
                else
                {
                    return RedirectToBasics();
                }
            }
            catch (Exception ex)
            {
                logger.LogError($"Exception @ POST [{nameof(ConfigureMonaIntegration)}: [{ex.Message}]");

                throw;
            }
        }

        [HttpGet, Route("config/seating", Name = RouteNames.ConfigureSeatingStrategy)]
        public async Task<IActionResult> ConfigureSeatingStrategy()
        {
            try
            {
                var publisherConfig = await publisherConfigClient.GetConfiguration();

                if (publisherConfig?.IsSetupComplete == true)
                {
                    this.ApplyModel(new LayoutViewModel(publisherConfig));

                    return View(new SeatingConfigurationViewModel(publisherConfig!));
                }
                else
                {
                    return RedirectToBasics();
                }
            }
            catch (Exception ex)
            {
                logger.LogError($"Exception @ GET [{nameof(ConfigureSeatingStrategy)}]: [{ex.Message}].");

                throw;
            }
        }

        [HttpPost, Route("config/seating")]
        public async Task<IActionResult> ConfigureSeatingStrategy([FromForm] SeatingConfigurationViewModel seatingConfig)
        {
            try
            {
                var publisherConfig = await publisherConfigClient.GetConfiguration();

                if (publisherConfig?.IsSetupComplete == true)
                {
                    if (ModelState.IsValid)
                    {
                        publisherConfig!.Apply(seatingConfig);

                        await publisherConfigClient.UpdateConfiguration(publisherConfig!);

                        seatingConfig.IsConfigurationSaved = true;
                        seatingConfig.HasValidationErrors = false;
                    }
                    else
                    {
                        seatingConfig.IsConfigurationSaved = false;
                        seatingConfig.HasValidationErrors = true;
                    }

                    this.ApplyModel(new LayoutViewModel(publisherConfig));

                    return View(seatingConfig);
                }
                else
                {
                    return RedirectToBasics();
                }
            }
            catch (Exception ex)
            {
                logger.LogError($"Exception @ POST [{nameof(ConfigureSeatingStrategy)}: [{ex.Message}]");

                throw;
            }
        }

        private IActionResult RedirectToBasics() => Redirect(RouteNames.ConfigureBasics);
    }
}
