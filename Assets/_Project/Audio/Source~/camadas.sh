#!/bin/sh
# Camadas de tensão do golpe, tiradas da própria Heavy Heist (mesmo tamanho, então nunca saem do tempo):
#   Heist_1 (alerta 35%): só os agudos (pratos e caixa), comprimidos pra ficarem na cara.
#   Heist_2 (alerta 70%): só os graves (bumbo e baixo), comprimidos pra pesar.
# Volume: as camadas ficam uns 6 dB abaixo da base no jogo (a base importa com Normalize, que sobe uns 2 dB).
# Uso (da raiz do repositório): sh "Assets/_Project/Audio/Source~/camadas.sh"   (precisa de ffmpeg com libmp3lame)
# As camadas importam com Normalize desligado (.meta), senão a Unity sobe tudo pro pico de novo.
set -e
M="Assets/_Project/Audio/Resources/Musica"
ffmpeg -y -v error -i "$M/Heist.mp3" -af "highpass=f=2500,highpass=f=2500,acompressor=threshold=0.05:ratio=8:attack=2:release=60:makeup=4,volume=-8dB,alimiter=limit=0.9" -c:a libmp3lame -b:a 160k "$M/Heist_1.mp3"
ffmpeg -y -v error -i "$M/Heist.mp3" -af "lowpass=f=180,lowpass=f=180,acompressor=threshold=0.08:ratio=10:attack=8:release=120:makeup=6,volume=-8dB,alimiter=limit=0.9" -c:a libmp3lame -b:a 160k "$M/Heist_2.mp3"
