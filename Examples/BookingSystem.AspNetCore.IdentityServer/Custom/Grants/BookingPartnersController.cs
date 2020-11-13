// Copyright (c) Brock Allen & Dominick Baier. All rights reserved.
// Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.


using IdentityServer4.Services;
using IdentityServer4.Stores;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using IdentityServer4.Events;
using IdentityServer4.Extensions;
using OpenActive.FakeDatabase.NET;
using System.Security.Cryptography;
using System;
using IdentityServer4.Models;
using System.Text.RegularExpressions;

namespace src
{
    /// <summary>
    /// This sample controller allows a user to revoke grants given to clients
    /// </summary>
    [SecurityHeaders]
    [Authorize]
    public class BookingPartnersController : Controller
    {
        private readonly IIdentityServerInteractionService _interaction;
        private readonly IClientStore _clients;
        private readonly IResourceStore _resources;
        private readonly IEventService _events;

        public BookingPartnersController(IIdentityServerInteractionService interaction,
            IClientStore clients,
            IResourceStore resources,
            IEventService events)
        {
            _interaction = interaction;
            _clients = clients;
            _resources = resources;
            _events = events;
        }

        /// <summary>
        /// Show list of grants
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            return View("Index", await BuildViewModelAsync());
        }

        /// <summary>
        /// Show list of grants
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> Edit(string Id)
        {
            return View("BookingPartnerEdit", await BuildBookingPartnerViewModelAsync(Id));
        }

        /// <summary>
        /// Show list of grants
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> Create()
        {
            return View("BookingPartnerCreate", await Task.FromResult(new BookingPartnerModel()));
        }

        /// <summary>
        /// Add a new booking partner
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateBookingPartner(string email, string bookingPartnerName)
        {
            var hmac = new HMACSHA256();
            var key = Convert.ToBase64String(hmac.Key);

            Regex rgx = new Regex("[^a-z0-9 ]");
            var humanKey = rgx.Replace(bookingPartnerName.ToLowerInvariant(), "").Replace(' ', '_') + "_" + key;

            var newBookingPartner = new BookingPartnerTable()
            {
                ClientId = null,
                ClientSecret = null,
                Email = email,
                Registered = false,
                RegistrationKey = humanKey,
                RegistrationKeyValidUntil = DateTime.Now.AddDays(2),
                CreatedDate = DateTime.Now,
                BookingsSuspended = false,
                ClientProperties = new ClientModel
                {
                    ClientName = bookingPartnerName
                }
            };

            FakeBookingSystem.Database.AddBookingPartner(newBookingPartner);

            return View("BookingPartnerEdit", await BuildBookingPartnerViewModelAsync(newBookingPartner.ClientId));
        }

        /// <summary>
        /// Handle postback to revoke a client
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Revoke(string clientId)
        {
            await _interaction.RevokeUserConsentAsync(clientId);
            await _events.RaiseAsync(new GrantsRevokedEvent(User.GetSubjectId(), clientId));

            return RedirectToAction("Index");
        }

        /// <summary>
        /// Handle postback to suspend a client
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Suspend(string clientId)
        {
            var client = await _clients.FindClientByIdAsync(clientId);
            client.AllowedScopes.Remove("openactive-openbooking");
            await _events.RaiseAsync(new GrantsRevokedEvent(User.GetSubjectId(), clientId));

            FakeBookingSystem.Database.UpdateBookingPartnerScope(
                clientId,
                "openid profile openactive-ordersfeed",
                true
                );

            return RedirectToAction("Index");
        }

        /// <summary>
        /// Handle postback to generate a registration key
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RegenerateKey(string clientId)
        {
            var hmac = new HMACSHA256();
            var key = Convert.ToBase64String(hmac.Key);

            FakeBookingSystem.Database.SetBookingPartnerKey(clientId, key, null);

            return View("BookingPartnerEdit", await BuildBookingPartnerViewModelAsync(clientId));
        }

        /// <summary>
        /// Handle postback to generate a registration key, and a new client secret
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RegenerateAllKeys(string clientId)
        {
            var hmac = new HMACSHA256();
            var registrationKey = Convert.ToBase64String(hmac.Key);

            var hmacSecret = new HMACSHA256();
            var clientSecret = Convert.ToBase64String(hmacSecret.Key);

            FakeBookingSystem.Database.SetBookingPartnerKey(clientId, registrationKey, clientSecret);

            var client = await _clients.FindClientByIdAsync(clientId);
            client.ClientSecrets = new List<Secret>() { new Secret(clientSecret.Sha256()) };

            return View("BookingPartnerEdit", await BuildBookingPartnerViewModelAsync(clientId));
        }

        private async Task<BookingPartnerModel> BuildBookingPartnerViewModelAsync(string clientId)
        {
            var client = await _clients.FindClientByIdAsync(clientId);
            var bookingPartner = FakeBookingSystem.Database.GetBookingPartner(clientId);

            return new BookingPartnerModel()
            {
                ClientId = client.ClientId,
                ClientName = client.ClientName ?? client.ClientId,
                ClientLogoUrl = bookingPartner.ClientProperties?.LogoUri,
                ClientUrl = bookingPartner.ClientProperties?.ClientUri,
                BookingPartner = bookingPartner
            };
        }
        private async Task<BookingPartnerViewModel> BuildViewModelAsync()
        {
            var bookingPartners = FakeBookingSystem.Database.GetBookingPartners();
            var list = new List<BookingPartnerModel>();
            foreach (var bookingPartner in bookingPartners)
            {
                var item = new BookingPartnerModel()
                {
                    ClientId = bookingPartner.ClientId,
                    ClientName = bookingPartner.ClientProperties?.ClientName ?? bookingPartner.ClientId,
                    ClientLogoUrl = bookingPartner.ClientProperties?.LogoUri,
                    ClientUrl = bookingPartner.ClientProperties?.ClientUri,
                    BookingPartner = bookingPartner
                };

                list.Add(item);
            }

            return new BookingPartnerViewModel
            {
                BookingPartners = list
            };
        }
    }
}