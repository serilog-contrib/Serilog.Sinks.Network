
semver = '1.2.0'

BUILD_CONFIG = ENV['Configuration'] || 'Release'
BUILD_NUMBER = ENV['APPVEYOR_BUILD_NUMBER'] || '0'

PACKAGE_VERSION = "#{semver}.#{BUILD_NUMBER}"

default_build_command = 'MSBuild'
BUILD_COMMAND = ENV['BUILD_COMMAND'] || default_build_command

directory 'Reports/UnitTests'

task :clean_packages do
  File.delete(*Dir.glob('./**/bin/*.nupkg'))
end

task :version do
  FileList['./**/Properties/AssemblyInfo.cs'].each do |assemblyfile|
    file = File.read(assemblyfile)
    new_contents = file.gsub(/AssemblyVersion\("\d\.\d\.\d\.\d"\)/, "AssemblyVersion(\"#{PACKAGE_VERSION}\")")
                       .gsub(/AssemblyFileVersion\("\d\.\d\.\d\.\d"\)/, "AssemblyFileVersion(\"#{PACKAGE_VERSION}\")")
    File.open(assemblyfile, "w") {|f| f.puts new_contents }
  end
end

def run_msbuild(target)
  command = [
    BUILD_COMMAND,
    './Serilog.Sinks.Network.sln',
    '/verbosity:minimal',
    "/property:configuration=\"#{BUILD_CONFIG}\"",
    '/property:VisualStudioVersion="14.0"',
    '/m',
    '/property:RunOctoPack="true"',
    "/target:\"#{target}\"",
    "/p:OctoPackPackageVersion=#{PACKAGE_VERSION}"
  ].join(' ')
  sh command
end

task clean: [:clean_packages] do 
  run_msbuild 'Clean' 
end

task :build do 
  run_msbuild 'Build'
end

task test: 'Reports/UnitTests' do
  test_dlls = FileList['./**/bin/**/*.Test.dll'].join(' ')
  command = [
    './packages/xunit.runner.console.2.1.0/tools/xunit.console.exe',
    "#{test_dlls}",
    '-parallel none ',
    '-nologo',
    '-xml Reports/UnitTests/TestResults.xml'
  ].join(' ')
  sh(command)
end

task default: [:version, :clean, :build, :test]
