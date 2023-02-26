// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Turnstile.Core.Interfaces;
using Turnstile.Web.Extensions;
using Turnstile.Web.Models;

namespace Turnstile.Web.Controllers;

[Authorize]
public class PublisherConfigurationController : Controller
{
    public static class RouteNames
    {
        public const string GetPublisherConfiguration = "get_publisher_config";
        public const string PostPublisherConfiguration = "post_publisher_config";
    }

    private readonly ILogger logger;
    private readonly IPublisherConfigurationClient pubConfigClient;

    public PublisherConfigurationController(
        ILogger<PublisherConfigurationController> logger,
        IPublisherConfigurationClient pubConfigClient)
    {
        this.logger = logger;
        this.pubConfigClient = pubConfigClient;
    }

    [HttpGet]
    [Route("publisher/setup", Name = RouteNames.GetPublisherConfiguration)]
    public async Task<IActionResult> PublisherSetup()
    {
        try
        {
            if (User.CanAdministerTurnstile())
            {
                var pubConfig = await pubConfigClient.GetConfiguration();

                if (pubConfig == null)
                {
                    return View(new PublisherConfigurationViewModel());
                }
                else
                {
                    this.ApplyLayout(pubConfig, User!);

                    return View(new PublisherConfigurationViewModel(pubConfig));
                }
            }
            else
            {
                return Forbid();
            }
        }
        catch (Exception ex)
        {
            logger.LogError($"Exception @ GET [{nameof(PublisherSetup)}: [{ex.Message}]");

            throw;
        }
    }

    [HttpPost]
    [Route("publisher/setup")]
    public async Task<IActionResult> PublisherSetup([FromForm] PublisherConfigurationViewModel pubConfigModel)
    {
        try
        {
            if (User.CanAdministerTurnstile())
            {
                if (ModelState.IsValid)
                {
                    await pubConfigClient.UpdateConfiguration(pubConfigModel.ToCoreModel(true));

                    pubConfigModel.HasValidationErrors = false;
                    pubConfigModel.IsConfigurationSaved = true;
                }
                else
                {
                    pubConfigModel.HasValidationErrors = true;
                    pubConfigModel.IsConfigurationSaved = false;
                }

                var pubConfig = await pubConfigClient.GetConfiguration();

                if (pubConfig != null)
                {
                    this.ApplyLayout(pubConfig, User!);
                }

                return View(nameof(PublisherSetup), pubConfigModel);
            }
            else
            {
                return Forbid();
            }
        }
        catch (Exception ex)
        {
            logger.LogError($"Exception @ POST [{nameof(PublisherSetup)}: [{ex.Message}]");

            throw;
        }
    }    
}
