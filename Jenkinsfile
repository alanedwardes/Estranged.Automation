properties([pipelineTriggers([cron('H/15 * * * *')])])

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
				string(credentialsId: 'SyndicationWebHookUrl', variable: 'SYNDICATION_WEB_HOOK_URL'),
				string(credentialsId: 'ReviewsWebHookUrl', variable: 'REVIEWS_WEB_HOOK_URL'),
				file(credentialsId: 'GoogleComputeEstrangedAutomation', variable: 'GOOGLE_APPLICATION_CREDENTIALS'),
				[$class: 'AmazonWebServicesCredentialsBinding', credentialsId: 'JenkinsEstrangedAutomation']
			]) {
				sh "dotnet run --project src/Automation"
			}
		}
	}
}