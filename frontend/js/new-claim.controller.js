angular.module('warrantyApp').controller('NewClaimController', function (ClaimsService, $location) {
    var vm = this;
    vm.form = { parts: [{}, {}] };
    vm.manufacturers = [];

    // the dropdown that makes the app LOOK multi-manufacturer - populated from
    // GET /api/manufacturers, which happily returns Kestrel/Solenne/Northgate alongside Bramwell.
    // picking one of them doesn't actually do anything different server-side -
    // see Services/ManufacturerRulesService.cs.
    ClaimsService.manufacturers().then(function (data) { vm.manufacturers = data; });

    vm.addPart = function () {
        if (vm.form.parts.length >= 5) { alert('Max 5 parts - put extras in the notes.'); return; }
        vm.form.parts.push({});
    };

    vm.save = function () {
        ClaimsService.create(vm.form).then(function (claim) {
            $location.path('/claims/' + claim.id);
        });
    };
});
