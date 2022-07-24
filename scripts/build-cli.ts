import caporal from "caporal";
import { build, Flavor, Platform } from "./build";

caporal
  .version("1.0.0")
  .command("local", "Builds and publishes local nuget versions")
  .action(buildLocal);

caporal.parse(process.argv);

async function buildLocal() {
  await build({
    flavor: Flavor.Debug,
    targets: ["Pack"],
  });
}
