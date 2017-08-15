semver = '2.0.0'

BUILD_CONFIG = ENV['Configuration'] || 'Release'
BUILD_NUMBER = ENV['APPVEYOR_BUILD_NUMBER'] || '0'

PACKAGE_VERSION = "#{semver}.#{BUILD_NUMBER}"

directory 'Reports/UnitTests'

task :clean_packages do
  File.delete(*Dir.glob('./**/bin/*.nupkg'))
end

task clean: [:clean_packages] do
  sh 'dotnet clean'
end

task :restore do
  sh 'dotnet restore'
end

task :build do
  command = [
    'dotnet',
    'pack',
    "--configuration=#{BUILD_CONFIG}",
    "/p:Version=#{PACKAGE_VERSION}"
  ].join(' ')
  sh command
end

task test: 'Reports/UnitTests' do
  Dir.chdir('Serilog.Sinks.Network.Test') do
   sh 'dotnet xunit -parallel none -xml ../Reports/UnitTests/TestResults.xml'
  end
end

task default: [:clean, :restore, :build, :test]
