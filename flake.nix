{
  description = "A Nix-flake-based C# development environment";

  inputs.nixpkgs.url = "github:NixOS/nixpkgs/nixpkgs-unstable";

  outputs = { self, nixpkgs }:
    let
      supportedSystems = [ "x86_64-linux" "aarch64-linux" "x86_64-darwin" "aarch64-darwin" ];
      forEachSupportedSystem = f: nixpkgs.lib.genAttrs supportedSystems (system: f {
        pkgs = import nixpkgs { inherit system; };
      });
    in
    {
      devShells = forEachSupportedSystem ({ pkgs }: {
        default = pkgs.mkShell {
          name = "dotnet-env";
          packages = with pkgs; [
            (with dotnetCorePackages; combinePackages [sdk_8_0])

            msbuild

            vscode-langservers-extracted
            omnisharp-roslyn
            yaml-language-server
          ];
           shellHook = ''
           export PATH=$PATH:~/.dotnet/tools
           export DOTNET_ROOT=${pkgs.dotnetCorePackages.sdk_8_0}
           '';
        };
      });
    };
}
