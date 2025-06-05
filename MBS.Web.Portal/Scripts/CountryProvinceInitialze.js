function CountryProvinceViewOnLoad() {
    var countryProvinceMap = new Array();
    countryProvinceMap[0] = { country: "100000000", from: "100000000", to: "100000012", defaultProv: "100000000" }; // Canada
    countryProvinceMap[1] = { country: "100000001", from: "100000100", to: "100000149", defaultProv: "100000100" }; // United States

    InitializeCountryProvince("Country", "Province", countryProvinceMap);

    var countryPostalCodeMap = new Array();
    countryPostalCodeMap[0] = { country: "100000000", mask: "?a9a 9a9", example: "S4T 0G6" };  // Canada
    countryPostalCodeMap[1] = { country: "100000001", mask: "?99999", example: "12345" }; // United States

    InitializePostalCodeMask("Country", "PostalCode", countryPostalCodeMap);
}