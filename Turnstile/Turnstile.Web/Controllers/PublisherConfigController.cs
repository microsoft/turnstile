// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Turnstile.Core.Interfaces;
using Turnstile.Core.Models.Configuration;
using Turnstile.Web.Extensions;
using Turnstile.Web.Models;
using Turnstile.Web.Models.PublisherConfig;

namespace Turnstile.Web.Controllers
{
    [Authorize]
    public class PublisherConfigController : Controller
    {
        public static class RouteNames
        {
            public const string ConfigureBasics = "configure_basics";
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
                if (User.CanAdministerTurnstile())
                {
                    var publisherConfig = await publisherConfigClient.GetConfiguration();

                    if (publisherConfig == null)
                    {
                        return View(new BasicConfigurationViewModel());
                    }
                    else
                    {
                        ViewData.ApplyModel(new LayoutViewModel(publisherConfig, User));

                        return View(new BasicConfigurationViewModel(publisherConfig));
                    }
                }
                else
                {
                    return Forbid();
                }
            }
            catch (Exception ex)
            {
                logger.LogError($"Exception @ GET [{nameof(ConfigureBasics)}]: [{ex.Message}].");

                throw;
            }
        }

        [HttpPost, Route("config/basics")]
        public async Task<IActionResult> ConfigureBasics([FromForm] BasicConfigurationViewModel basicConfig)
        {
            try
            {
                if (User.CanAdministerTurnstile())
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

                    ViewData.ApplyModel(new LayoutViewModel(publisherConfig!, User));

                    return View(basicConfig);
                }
                else
                {
                    return Forbid();
                }
            }
            catch (Exception ex)
            {
                logger.LogError($"Exception @ POST [{nameof(ConfigureBasics)}: [{ex.Message}]");

                throw;
            }
        }

        [HttpGet, Route("config/redirection", Name = RouteNames.ConfigureUserRedirection)]
        public async Task<IActionResult> ConfigureUserRedirection()
        {
            try
            {
                if (User.CanAdministerTurnstile())
                {
                    var publisherConfig = await publisherConfigClient.GetConfiguration();

                    if (publisherConfig!.CheckTurnstileSetupIsComplete(User, logger) is var setupAction &&
                        setupAction != null)
                    {
                        return setupAction;
                    }
                    else
                    {
                        ViewData.ApplyModel(new LayoutViewModel(publisherConfig!, User));

                        return View(new RedirectConfigurationViewModel(publisherConfig!));
                    }
                }
                else
                {
                    return Forbid();
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
                if (User.CanAdministerTurnstile())
                {
                    var publisherConfig = await publisherConfigClient.GetConfiguration();

                    if (publisherConfig!.CheckTurnstileSetupIsComplete(User, logger) is var setupAction &&
                        setupAction != null)
                    {
                        return setupAction;
                    }
                    else if (ModelState.IsValid)
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

                    ViewData.ApplyModel(new LayoutViewModel(publisherConfig!, User));

                    return View(redirectConfig);
                }
                else
                {
                    return Forbid();
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
                if (User.CanAdministerTurnstile())
                {
                    var publisherConfig = await publisherConfigClient.GetConfiguration();

                    if (publisherConfig!.CheckTurnstileSetupIsComplete(User, logger) is var setupAction &&
                        setupAction != null)
                    {
                        return setupAction;
                    }
                    else
                    {
                        ViewData.ApplyModel(new LayoutViewModel(publisherConfig!, User));

                        return View(new MonaConfigurationViewModel(publisherConfig!));
                    }
                }
                else
                {
                    return Forbid();
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
                if (User.CanAdministerTurnstile())
                {
                    var publisherConfig = await publisherConfigClient.GetConfiguration();

                    if (publisherConfig!.CheckTurnstileSetupIsComplete(User, logger) is var setupAction &&
                        setupAction != null)
                    {
                        return setupAction;
                    }
                    else if (ModelState.IsValid)
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

                    ViewData.ApplyModel(new LayoutViewModel(publisherConfig!, User));

                    return View(monaConfig);
                }
                else
                {
                    return Forbid();
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
                if (User.CanAdministerTurnstile())
                {
                    var publisherConfig = await publisherConfigClient.GetConfiguration();

                    if (publisherConfig!.CheckTurnstileSetupIsComplete(User, logger) is var setupAction &&
                        setupAction != null)
                    {
                        return setupAction;
                    }
                    else
                    {
                        ViewData.ApplyModel(new LayoutViewModel(publisherConfig!, User));

                        return View(new SeatingConfigurationViewModel(publisherConfig!));
                    }
                }
                else
                {
                    return Forbid();
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
                if (User.CanAdministerTurnstile())
                {
                    var publisherConfig = await publisherConfigClient.GetConfiguration();

                    if (publisherConfig!.CheckTurnstileSetupIsComplete(User, logger) is var setupAction &&
                        setupAction != null)
                    {
                        return setupAction;
                    }
                    else if (ModelState.IsValid)
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

                    ViewData.ApplyModel(new LayoutViewModel(publisherConfig!, User));

                    return View(seatingConfig);
                }
                else
                {
                    return Forbid();
                }
            }
            catch (Exception ex)
            {
                logger.LogError($"Exception @ POST [{nameof(ConfigureSeatingStrategy)}: [{ex.Message}]");

                throw;
            }
        }
    }
}
