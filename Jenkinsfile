properties([pipelineTriggers([cron('H/6 * * * *')])])

node("linux") {
    timestamps {
        stage ("Checkout") {
            checkout scm
        }

        stage ("Restore") {
            sh "dotnet restore"
        }

        stage ("Build") {
            sh "dotnet build"
        }

        stage ("Run") {
            withCredentials([
                string(credentialsId: 'SlackWebHookUrl', variable: 'SLACK_WEB_HOOK_URL'),
                [$class: 'AmazonWebServicesCredentialsBinding', credentialsId: 'JenkinsEstrangedAutomation']
            ]) {
                sh "dotnet run --project src/Automation"
            }
        }
    }
}