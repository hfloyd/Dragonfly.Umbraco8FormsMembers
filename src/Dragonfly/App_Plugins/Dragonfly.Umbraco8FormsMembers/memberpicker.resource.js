function memberPickerResource($http) {

    var apiRoot = "/Umbraco/backoffice/Umbraco8FormsMembers/Member/";

    return {
 
        getAllMemberTypesWithAlias: function () {
            return $http.get(apiRoot + "GetAllMemberTypesWithAlias");
        },
        getAllProperties: function (membertypeAlias) {
            return $http.get(apiRoot + "GetAllProperties?membertypeAlias=" + membertypeAlias);
        }

    };
}

angular.module('umbraco.resources').factory('memberPickerResource', memberPickerResource);