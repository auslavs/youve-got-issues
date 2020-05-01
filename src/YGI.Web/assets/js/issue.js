
Vue.config.devtools = true;

$('#project').text(window.location.pathname.substring(1)),

  Vue.filter('formatDate', function (value) {
    if (value) {
      return moment(String(value)).format('DD/MM/YYYY')
    }
  });

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
      model: null,
      issueTypes: null,
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
    }
  },
  methods: {
    loadIssue: function () {
      axios
        .get('/api' + this.path)
        .then(response => (
          this.model = response.data,
          this.issue = response.data.issue,
          this.issueTypes = response.data.issueTypes,
          this.equipmentTypes = response.data.equipmentTypes,
          this.areaList = response.data.areaList
        ))
    }
  },
  mounted() {
    this.loadIssue()
  }
})