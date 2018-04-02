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
            sh "dotnet run --project src/Automation"
        }
    }
}