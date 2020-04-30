Vue.config.devtools = true;
    
      Vue.filter('formatDate', function (value) {
        if (value) {
          return moment(String(value)).format('DD/MM/YYYY')
        }
      });
    
      new Vue({
        el: '#project-list',
        data() {
          return {
            model: null,
            projects: null,
            newProject: {
              ProjectNumber: null,
              ProjectName: null
            }
          }
        },
    
        methods: {
    
          loadProjects: function () {
            axios
              .get('/api/summary')
              .then(response => (
                this.model = response.data,
                this.projects = response.data.projList
              ))
          },
          submitProject: function () {
            axios
              .post('/api/' + this.newProject.ProjectNumber, this.newProject)
              .then(res => 
                this.loadProjects(),
                $('#new-project-modal').modal('hide'),
                this.newProject = 
                  {
                    ProjectNumber: null,
                    ProjectName: null
                  }
                )
              .catch(function (error) {
                console.log(error)
              })
          }
        },
        mounted() {
          this.loadProjects()
        }
      })