properties([
	pipelineTriggers([cron('H * * * *')]),
	disableConcurrentBuilds()
])

node("linux") {
	timestamps {
		stage ("Checkout") {
			checkout scm
		}

		stage ("Run") {
			withCredentials([
				string(credentialsId: 'CommunityWebHookUrl', variable: 'COMMUNITY_WEB_HOOK_URL'),
				string(credentialsId: 'SlackWebHookUrl', variable: 'SLACK_WEB_HOOK_URL'),
				string(credentialsId: 'SyndicationWebHookUrl', variable: 'SYNDICATION_WEB_HOOK_URL'),
				string(credentialsId: 'ReviewsWebHookUrl', variable: 'REVIEWS_WEB_HOOK_URL'),
				string(credentialsId: 'DiscordBotToken', variable: 'DISCORD_BOT_TOKEN'),
				file(credentialsId: 'GoogleComputeEstrangedAutomation', variable: 'GOOGLE_APPLICATION_CREDENTIALS'),
				[$class: 'AmazonWebServicesCredentialsBinding', credentialsId: 'JenkinsEstrangedAutomation']
			]) {
				sh "dotnet run --project src/Automation"
			}
		}
	}
}