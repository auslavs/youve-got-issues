
Vue.config.devtools = true;

$('#project').text(window.location.pathname.substring(1)),

Vue.filter('formatDate', function (value) {
  if (value) {
    return moment(String(value)).format('DD/MM/YYYY')
  }
});

Vue.filter('removeProject', function (value) {
  if (value) {
    var str = String(value)
    return str.substring(str.indexOf("/") + 1);
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
      files: [],
      uploadProgress:0,
      uploadProgressVisible:false,
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
        attachments: [],
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
    addFiles(){
      this.$refs.files.click();
    },
    uploadFiles(){
      this.uploadProgressVisible = true;
      this.files = this.$refs.files.files;
      var size = 0;
      let formData = new FormData();

      for( var i = 0; i < this.files.length; i++ ){
        let file = this.files[i];
        size = size + file.size;
        formData.append('files[' + i + ']', file);
      }

      axios.put('/api' + this.path +'/upload',
        formData,
        {
          headers: {
              'Content-Type': 'multipart/form-data'
          },
          onUploadProgress: progressEvent => {
            this.uploadProgress = ((progressEvent.loaded/size) * 100).toFixed(0)
            console.log(this.uploadProgress);
           } 
        }
      ).then(response => (
        this.uploadProgressVisible = false,
        this.uploadProgress = 0,
        this.files = [],
        this.loadIssue()
      ))
      .catch(function (error) {
        console.log(error)
      })
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
        .catch(function (error) {
          console.log(error)
        })
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
  computed: {
    widthPercentage() {
        return this.uploadProgress + "%";
    }
},
  mounted() {
    this.loadIssue()
  }
})