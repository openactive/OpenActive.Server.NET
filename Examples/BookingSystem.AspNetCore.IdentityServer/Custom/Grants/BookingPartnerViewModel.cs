// Copyright (c) Brock Allen & Dominick Baier. All rights reserved.
// Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.

using OpenActive.FakeDatabase.NET;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace src
{
    public class BookingPartnerViewModel
    {
        public IEnumerable<BookingPartnerModel> BookingPartners { get; set; }

        public static async Task<BookingPartnerViewModel> Build(long? sellerUserId = null)
        {
            var bookingPartners = sellerUserId.HasValue
                ? await BookingPartnerTable.GetBySellerUserId(sellerUserId.Value)
                : await BookingPartnerTable.Get();
            var list = (await Task.WhenAll(bookingPartners.Select(async bookingPartner =>
            {
                var bookingStatistics = await BookingStatistics.Get(bookingPartner.ClientId);
                return new BookingPartnerModel
                {
                    ClientId = bookingPartner.ClientId,
                    ClientName = bookingPartner.Name,
                    ClientLogoUrl = bookingPartner.LogoUri,
                    ClientUrl = bookingPartner.ClientUri,
                    RestoreAccessUrl = bookingPartner.RestoreAccessUri,
                    BookingPartner = bookingPartner,
                    SellersEnabled = bookingStatistics.SellersEnabled,
                    BookingsByBroker = bookingStatistics.BookingsByBroker
                };
            }))).ToList();

            return new BookingPartnerViewModel { BookingPartners = list };
        }
    }

    public class BookingPartnerModel
    {
        public string ClientId { get; set; }
        public string ClientName { get; set; }
        public string ClientUrl { get; set; }
        public string RestoreAccessUrl { get; set; }
        public string ClientLogoUrl { get; set; }
        public DateTime Created { get; set; }
        public DateTime? Expires { get; set; }
        public IEnumerable<string> IdentityGrantNames { get; set; }
        public IEnumerable<string> ApiGrantNames { get; set; }
        public BookingPartnerTable BookingPartner { get; set; }
        public IDictionary<string, int> BookingsByBroker { get; set; }
        public int SellersEnabled { get; set; }
    }
}