angular.module('warrantyApp').controller('ClaimsListController', function (ClaimsService) {
    var vm = this;
    vm.claims = [];
    vm.q = '';

    function load() {
        ClaimsService.list().then(function (data) { vm.claims = data; });
    }

    vm.search = function () {
        if (!vm.q) { load(); return; }
        // hits GET /api/claims/search?q=... which is built with FromSqlRaw and string
        // interpolation server-side - see ClaimsController.cs. this UI is the only
        // thing standing between a user and that endpoint, and it's not standing very
        // hard: try searching for  ' OR '1'='1
        ClaimsService.search(vm.q).then(function (data) { vm.claims = data; });
    };

    load();
});
