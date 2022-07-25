import caporal from "caporal";
import { build } from "./build";
import { push } from "./nuget";
import { buildOptions, Flavor } from "./options";

const cli = caporal.version("1.0.0");
cli
  .command("local", "Builds and publishes local nuget versions")
  .option("-n, --no-build", "No build")
  .action(buildLocal);

cli.parse(process.argv);

interface CliArgs {
  noBuild?: boolean;
}

async function buildLocal(_: any, args: CliArgs, _logger: any) {
  const opts = buildOptions({
    flavor: Flavor.Debug,
    targets: ["Pack"],
  });
  if (!args.noBuild) {
    await build(opts);
  }
  await push(opts);
}
