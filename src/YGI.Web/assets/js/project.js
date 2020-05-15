Vue.config.devtools = true;

$('#current-page').text(window.location.pathname.substring(1));

Vue.filter('formatDate', function (value) {
  if (value) {
    return moment(String(value)).format('DD/MM/YYYY')
  }
});

new Vue({
  el: '#issues-list',
  data() {
    return {
      loading: true,
      model: null,
      issues: null,
      issueTypes: null,
      equipmentTypes: null,
      areaList: null,
      statusTypes:null,
      path: window.location.pathname,
      newIssue: {
        Area: null,
        Equipment: null,
        IssueType: null,
        Title: null,
        Description: null,
        RaisedBy: null
      }
    }
  },
  methods: {
    loadIssues: function () {
      axios
        .get('/api' + this.path)
        .then(response => (
          this.model = response.data,
          this.issues = response.data.issues,
          this.issueTypes = response.data.issueTypes,
          this.statusTypes = response.data.statusTypes,
          this.equipmentTypes = response.data.equipmentTypes,
          this.areaList = response.data.areaList,
          this.loading = false,
          this.UpdatePicker("#area-picker", response.data.areaList),
          this.UpdatePicker("#equipment-picker", response.data.equipmentTypes),
          this.UpdatePicker("#issue-picker", response.data.issueTypes),
          this.UpdatePicker("#status-picker", response.data.statusTypes)
        ))
    },
    UpdatePicker: function (picker,options) {
      var $el = $(picker);
      $el.empty();
      $.each(options, function(key,value) {
        $el.append($("<option></option>")
          .attr("value", value).text(value));
      });
      $el.selectpicker('refresh').trigger('change');
    },
    onPickerChange: function (event) {
      var $el = $(event.target);
      var selected = $el.selectpicker().selectedOptions
      console.log(selected);
      console.log(event.target.value);
    },
    submitIssue: function () {
      axios
        .post('/api' + this.path + '/issues', this.newIssue)
        .then(res =>
          this.loadIssues(),
          $('#new-issue-modal').modal('hide'),
          this.newIssue = {
            Area: null,
            Equipment: null,
            IssueType: null,
            Title: null,
            Description: null,
            RaisedBy: null
          }
        )
        .catch(function (error) {
          console.log(error)
        })
    }
  },
  mounted() {
    this.loadIssues()
  }
})