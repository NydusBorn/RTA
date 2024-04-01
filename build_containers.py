import subprocess

subprocess.run("docker container stop rta-nuxt rta-asp.net", shell=True)
subprocess.run("docker container rm rta-nuxt rta-asp.net", shell=True)
subprocess.run("docker rmi rta-nuxt rta-asp.net", shell=True)
subprocess.run("npm run build", shell=True, cwd="Frontend")
subprocess.run("docker compose up -d", shell=True)