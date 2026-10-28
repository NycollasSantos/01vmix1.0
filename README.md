# 01vmix para Windows

Este repositório contém o pacote pronto para instalar do 01vmix na pasta
[`dist/01vmix`](dist/01vmix).

## Baixar

1. Abra a página do repositório no GitHub: <https://github.com/NycollasSantos/01vmix1.0>.
2. Clique em **Code** e depois em **Download ZIP**.
3. No Explorador de Arquivos, clique com o botão direito no ZIP e escolha
   **Extrair Tudo**.

Depois de extrair, abra a pasta `dist\01vmix`.

## Instalar e executar

1. Na pasta `dist\01vmix`, clique com o botão direito em `Instalar.ps1` e
   escolha **Executar com PowerShell**.
2. Se o Windows bloquear o script, abra o PowerShell nessa pasta e execute:

   ```powershell
   powershell -NoProfile -ExecutionPolicy Bypass -File .\Instalar.ps1
   ```

3. Após a instalação, abra **01vmix** pelo Menu Iniciar.

A instalação é feita apenas para o usuário atual e não deve pedir privilégios
de administrador.

## Requisitos

- Windows 7 ou posterior; o programa é de 32 bits e também pode rodar no
  Windows de 64 bits.
- .NET Framework 3.5, que inclui o runtime 2.0 necessário ao programa. No
  Windows 10 ou 11, talvez seja necessário ativar o recurso **.NET Framework
  3.5 (inclui .NET 2.0 e 3.0)** em **Ativar ou desativar recursos do Windows**.
- Para conectar a um console Yamaha DME, instale separadamente um driver MIDI
  compatível, como o driver de rede Yamaha DME.

## Desinstalar

Use **Configurações > Aplicativos > 01vmix** no Windows, ou execute
`Desinstalar.ps1` dentro da pasta instalada. A desinstalação preserva os dados
do usuário em `%APPDATA%` e as preferências em `%LOCALAPPDATA%`.

Consulte [`dist/01vmix/LEIA-ME.txt`](dist/01vmix/LEIA-ME.txt) e
[`dist/01vmix/LICENSE`](dist/01vmix/LICENSE) para mais informações.
