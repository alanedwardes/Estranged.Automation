node("linux") {
    timestamps {
        stage ("Checkout") {
            scm checkout
        }

        stage ("Restore") {
            sh "dotnet restore"
        }

        stage ("Build") {
            sh "dotnet build"
        }

        stage ("Run") {
            sh "dotnet run src/Automation"
        }
    }
}