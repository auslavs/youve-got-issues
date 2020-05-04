Vue.config.devtools = true;

$('#current-page').text(window.location.pathname.substring(1)),

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
          this.equipmentTypes = response.data.equipmentTypes,
          this.areaList = response.data.areaList,
          this.loading = false
        ))
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