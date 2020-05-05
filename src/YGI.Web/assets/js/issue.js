
Vue.config.devtools = true;

$('#project').text(window.location.pathname.substring(1)),

  Vue.filter('formatDate', function (value) {
    if (value) {
      return moment(String(value)).format('DD/MM/YYYY')
    }
  });

function toIssueUpdate(issue) {
  var update = {
    itemNo: issue.itemNo,
    title: issue.title,
    description: issue.description,
    area: issue.area,
    equipment: issue.equipment,
    issueType: issue.issueType,
    status: issue.status,
    vid: issue.vid
  };
  return update;
}

new Vue({
  el: '#breadcrumb',
  data() {
    return {
      breadcrumb: null,
    }
  },
  methods: {
    buildBreadcrumbPath: function () {

      var path = window.location.pathname.split("/")
      path.shift();

      var link = "/"
      var crumbs = new Array();
      for (var i = 0; i < path.length; i++) {
        link += path[i]
        let active = (i + 1) === path.length ? true : false;
        var crumb = { link: link, text: path[i], active: active }
        crumbs.push(crumb);
        link += "/"
      }
      this.breadcrumb = crumbs
    }
  },
  mounted() {
    this.buildBreadcrumbPath()
  }
})

new Vue({
  el: '#issues-detail',
  data() {
    return {
      loading: true,
      isEditing: false,
      model: null,
      issueTypes: null,
      statusTypes: null,
      equipmentTypes: null,
      areaList: null,
      path: window.location.pathname,
      issue:
      {
        area: null,
        Equipment: null,
        issueType: null,
        issue: null,
        raisedBy: null,
        raised: null,
        lastChanged: null,
        status: null,
        comments: null,
        vid: null
      },
      issueUpdate:
      {
        itemNo:null,
        title: null,
        description: null,
        area: null,
        Equipment: null,
        issueType: null,
        status: null,
        vid: null
      }
    }
  },
  methods: {
    cancelEditing: function () {
      this.isEditing = false,
      this.issueUpdate = toIssueUpdate(this.issue)
    },
    loadIssue: function () {
      axios
        .get('/api' + this.path)
        .then(response => (
          this.model = response.data,
          this.issue = response.data.issue,
          this.issueUpdate = toIssueUpdate(response.data.issue),
          this.issueTypes = response.data.issueTypes,
          this.statusTypes = response.data.statusTypes,
          this.equipmentTypes = response.data.equipmentTypes,
          this.areaList = response.data.areaList,
          this.loading = false
        ))
    },
    updateIssue: function () {
      axios
        .put('/api' + this.path, this.issueUpdate)
        .then(res =>
          this.loadIssue(),
          this.isEditing = false,
          this.issueUpdate = {
            title: null,
            description: null,
            area: null,
            Equipment: null,
            issueType: null,
            status: null,
            vid: null
          }
        )
        .catch(function (error) {
          console.log(error)
        })
    }
  },
  mounted() {
    this.loadIssue()
  }
})